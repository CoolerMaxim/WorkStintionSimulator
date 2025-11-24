using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using PipelineRunner.Steps;
using TelemetryGenerator.Generation;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddLogging()
    .AddTelemetryGeneration()
    .AddTransient<TelemetryGenerator.Cli.TelemetryGenerationRunner>();

builder.Services
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
