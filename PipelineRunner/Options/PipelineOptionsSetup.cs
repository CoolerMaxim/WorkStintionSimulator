using System.Linq;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Configuration;

namespace PipelineRunner.Options;

internal sealed class PipelineOptionsSetup : IPostConfigureOptions<PipelineOptions>
{
    private readonly SimulationSettings _simulationSettings;

    public PipelineOptionsSetup(IOptions<SimulationSettings> simulationOptions)
    {
        _simulationSettings = simulationOptions.Value;
    }

    public void PostConfigure(string? name, PipelineOptions options)
    {
        options.WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(options.WorkingDirectory);

        options.Scenario = string.IsNullOrWhiteSpace(options.Scenario) ? _simulationSettings.Scenario : options.Scenario;
        options.Duration = string.IsNullOrWhiteSpace(options.Duration) ? _simulationSettings.Duration : options.Duration;
        options.StepMinutes = options.StepMinutes <= 0 ? _simulationSettings.StepMinutes : options.StepMinutes;
        options.WorkstationId = string.IsNullOrWhiteSpace(options.WorkstationId) ? _simulationSettings.WorkstationId : options.WorkstationId;
        options.Difficulty = string.IsNullOrWhiteSpace(options.Difficulty) ? _simulationSettings.Difficulty : options.Difficulty;
        options.Start = string.IsNullOrWhiteSpace(options.Start) ? _simulationSettings.Start : options.Start;
        options.SpeakersConfigured = options.SpeakersConfigured <= 0 ? _simulationSettings.SpeakersConfigured : options.SpeakersConfigured;
        options.NodeProfile = string.IsNullOrWhiteSpace(options.NodeProfile) ? _simulationSettings.NodeProfile : options.NodeProfile;
        options.OutputPath = string.IsNullOrWhiteSpace(options.OutputPath) ? _simulationSettings.OutputPath : options.OutputPath;
        options.Seed ??= _simulationSettings.Seed;

        options.OutputPath = Normalize(options.OutputPath, options.WorkingDirectory);
        options.SolutionPath = Normalize(options.SolutionPath, options.WorkingDirectory);
        options.TelemetryProjectPath = Normalize(options.TelemetryProjectPath, options.WorkingDirectory);

        options.Steps ??= new List<string>();

        options.Steps = options.Steps
            .SelectMany(step => step.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Normalize(string path, string basePath)
    {
        return string.IsNullOrWhiteSpace(path)
            ? path
            : Path.GetFullPath(path, basePath);
    }
}
