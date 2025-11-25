using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Configuration;
using PipelineRunner.Options;
using PipelineRunner.Services;
using PipelineRunner.Steps;
using TelemetryGenerator.Cli;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.DataQualityChecker;

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
    .AddTelemetryGenerationRunner()
    .AddDataQualityChecker(builder.Configuration);

builder.Services
    .Configure<SimulationSettings>(builder.Configuration.GetSection("Simulation"))
    .AddOptions<PipelineOptions>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

builder.Services
    .AddSingleton<IValidateOptions<PipelineOptions>, PipelineOptionsValidator>()
    .AddSingleton<IPostConfigureOptions<PipelineOptions>, PipelineOptionsSetup>()
    .AddOptions<DatasetSettings>()
    .Bind(builder.Configuration.GetSection("Dataset"))
    .ValidateOnStart()
    .Services
    .AddOptions<ValidationSettings>()
    .Bind(builder.Configuration.GetSection("ValidationSettings"))
    .ValidateOnStart()
    .Services
    .AddOptions<TrainingSettings>()
    .Bind(builder.Configuration.GetSection("Training"))
    .ValidateOnStart()
    .Services
    .AddSingleton<IValidateOptions<DatasetSettings>, DatasetSettingsValidator>()
    .AddSingleton<IValidateOptions<ValidationSettings>, ValidationSettingsValidator>()
    .AddSingleton<IValidateOptions<TrainingSettings>, TrainingSettingsValidator>()
    .AddSingleton(sp => PipelineConfigFactory.Create(
        sp.GetRequiredService<IOptions<PipelineOptions>>().Value,
        sp.GetRequiredService<IOptions<DatasetSettings>>().Value,
        sp.GetRequiredService<IOptions<TrainingSettings>>().Value,
        sp.GetRequiredService<IOptions<SimulationSettings>>().Value))
    .AddScoped<BuildStep>()
    .AddScoped<TelemetryStep>()
    .AddScoped<QualityCheckStep>()
    .AddScoped<TrainStep>()
    .AddScoped<IDataQualityJob, DataQualityJob>()
    .AddScoped<ITrainingJob, TrainingJob>();

using var host = builder.Build();
var pipelineConfig = host.Services.GetRequiredService<PipelineConfig>();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Pipeline");

async Task<int> RunStepAsync(Func<IServiceProvider, Task<int>> executor)
{
    await using var scope = host.Services.CreateAsyncScope();
    return await executor(scope.ServiceProvider);
}

var steps = new List<(string Name, bool Enabled, Func<Task<int>> Action)>
{
    ("build", pipelineConfig.ShouldRunBuild, () => RunStepAsync(sp => sp.GetRequiredService<BuildStep>().ExecuteAsync())),
    ("telemetry", pipelineConfig.ShouldRunTelemetry, () => RunStepAsync(sp => sp.GetRequiredService<TelemetryStep>().ExecuteAsync())),
    ("check", pipelineConfig.ShouldRunCheck, () => RunStepAsync(sp => sp.GetRequiredService<QualityCheckStep>().ExecuteAsync())),
    ("train", pipelineConfig.ShouldRunTrain, () => RunStepAsync(sp => sp.GetRequiredService<TrainStep>().ExecuteAsync()))
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
