using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using PipelineRunner.Steps;
using TelemetryGenerator.Cli;
using TelemetryGenerator.Core.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddCommandLine(args, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "--all", nameof(PipelineOptions.RunAll) },
    { "--runAll", nameof(PipelineOptions.RunAll) },
    { "--build", nameof(PipelineOptions.Build) },
    { "--telemetry", nameof(PipelineOptions.Telemetry) },
    { "--check", nameof(PipelineOptions.Check) },
    { "--train", nameof(PipelineOptions.Train) },
    { "--steps", nameof(PipelineOptions.Steps) },
    { "--step", nameof(PipelineOptions.Steps) }
});

builder.Services
    .AddLogging()
    .AddTelemetryGenerationRunner();

builder.Services
    .Configure<SimulationSettings>(builder.Configuration.GetSection("Simulation"))
    .AddOptions<PipelineOptions>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services
    .AddSingleton<IValidateOptions<PipelineOptions>, PipelineOptionsValidator>()
    .AddSingleton<IPostConfigureOptions<PipelineOptions>, PipelineOptionsSetup>()
    .AddSingleton<BuildStep>()
    .AddSingleton<TelemetryStep>()
    .AddSingleton<QualityCheckStep>()
    .AddSingleton<TrainStep>();

using var host = builder.Build();
var options = host.Services.GetRequiredService<IOptions<PipelineOptions>>().Value;
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Pipeline");

var steps = new List<(string Name, bool Enabled, Func<Task<int>> Action)>
{
    ("build", options.ShouldRunBuild, () => host.Services.GetRequiredService<BuildStep>().ExecuteAsync()),
    ("telemetry", options.ShouldRunTelemetry, () => host.Services.GetRequiredService<TelemetryStep>().ExecuteAsync()),
    ("check", options.ShouldRunCheck, () => host.Services.GetRequiredService<QualityCheckStep>().ExecuteAsync()),
    ("train", options.ShouldRunTrain, () => host.Services.GetRequiredService<TrainStep>().ExecuteAsync())
};

foreach (var step in steps)
{
    if (!step.Enabled)
    {
        logger.LogInformation("[skip] {Step} (not requested)", step.Name);
        continue;
    }

    logger.LogInformation("=== {Step} ===", step.Name.ToUpperInvariant());
    var exit = await step.Action();
    logger.LogInformation("[{Step}] completed with exit code {ExitCode}", step.Name, exit);

    if (exit != 0)
    {
        return exit;
    }
}

return 0;
