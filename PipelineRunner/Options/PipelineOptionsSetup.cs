using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Configuration;

namespace PipelineRunner.Options;

internal sealed class PipelineOptionsSetup : IPostConfigureOptions<PipelineOptions>
{
    private readonly SimulationSettings _simulationSettings;
    private readonly string _contentRootPath;

    public PipelineOptionsSetup(IOptions<SimulationSettings> simulationOptions, IHostEnvironment environment)
    {
        _simulationSettings = simulationOptions.Value;
        _contentRootPath = environment.ContentRootPath;
    }

    public void PostConfigure(string? name, PipelineOptions options)
    {
        options.WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory)
            ? _contentRootPath
            : options.WorkingDirectory;

        options.WorkingDirectory = Path.GetFullPath(options.WorkingDirectory, _contentRootPath);

        options.Scenario = string.IsNullOrWhiteSpace(options.Scenario) ? _simulationSettings.Scenario : options.Scenario;
        options.Duration = string.IsNullOrWhiteSpace(options.Duration) ? _simulationSettings.Duration : options.Duration;
        options.StepMinutes = options.StepMinutes <= 0 ? _simulationSettings.StepMinutes : options.StepMinutes;
        options.WorkstationId = string.IsNullOrWhiteSpace(options.WorkstationId) ? _simulationSettings.WorkstationId : options.WorkstationId;
        options.Difficulty = string.IsNullOrWhiteSpace(options.Difficulty) ? _simulationSettings.Difficulty : options.Difficulty;
        options.Start = string.IsNullOrWhiteSpace(options.Start)
            ? string.IsNullOrWhiteSpace(_simulationSettings.Start)
                ? DateTime.UtcNow.ToString("o")
                : _simulationSettings.Start
            : options.Start;
        options.SpeakersConfigured = options.SpeakersConfigured <= 0 ? _simulationSettings.SpeakersConfigured : options.SpeakersConfigured;
        options.NodeProfile = string.IsNullOrWhiteSpace(options.NodeProfile) ? _simulationSettings.NodeProfile : options.NodeProfile;
        options.OutputPath = string.IsNullOrWhiteSpace(options.OutputPath) ? _simulationSettings.OutputPath : options.OutputPath;
        options.Seed ??= _simulationSettings.Seed;

        options.OutputPath = Normalize(options.OutputPath, options.WorkingDirectory);
        options.SolutionPath = EnsurePathExists(options.SolutionPath, options.WorkingDirectory);
        options.TelemetryProjectPath = EnsurePathExists(options.TelemetryProjectPath, options.WorkingDirectory);

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

    private static string EnsurePathExists(string path, string basePath)
    {
        var normalizedPath = Normalize(path, basePath);

        if (string.IsNullOrWhiteSpace(normalizedPath) || File.Exists(normalizedPath))
        {
            return normalizedPath;
        }

        var fileName = Path.GetFileName(normalizedPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return normalizedPath;
        }

        var currentDirectory = basePath;

        while (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            var candidate = Path.Combine(currentDirectory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(currentDirectory);
            if (parent == null || parent.FullName == currentDirectory)
            {
                break;
            }

            currentDirectory = parent.FullName;
        }

        return normalizedPath;
    }
}
