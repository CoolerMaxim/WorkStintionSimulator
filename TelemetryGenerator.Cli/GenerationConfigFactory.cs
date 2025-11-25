using System.Globalization;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Utilities;

namespace TelemetryGenerator.Cli;

public static class GenerationConfigFactory
{
    public static GenerationConfig Create(SimulationSettings settings, TelemetryGenerationRequest request)
    {
        var simulation = BuildSimulationConfig(settings, request);
        var outputPath = ResolveOutputPath(string.IsNullOrWhiteSpace(request.OutputPath) ? settings.OutputPath : request.OutputPath);

        return new GenerationConfig
        {
            Simulation = simulation,
            OutputPath = outputPath,
            Seed = request.Seed ?? settings.Seed
        };
    }

    private static SimulationConfig BuildSimulationConfig(SimulationSettings settings, TelemetryGenerationRequest request)
    {
        var difficulty = ParseDifficulty(request.Difficulty, settings.Difficulty);
        var start = ParseStart(string.IsNullOrWhiteSpace(request.Start) ? settings.Start : request.Start);
        var duration = ParseDuration(string.IsNullOrWhiteSpace(request.Duration) ? settings.Duration : request.Duration, settings.Duration);
        var stepMinutes = request.StepMinutes <= 0 ? settings.StepMinutes : request.StepMinutes;

        return new SimulationConfig
        {
            Scenario = string.IsNullOrWhiteSpace(request.Scenario) ? settings.Scenario : request.Scenario.Trim(),
            Difficulty = difficulty,
            Start = start,
            Duration = duration,
            Step = TimeSpan.FromMinutes(Math.Max(1, stepMinutes)),
            WorkstationId = string.IsNullOrWhiteSpace(request.WorkstationId) ? settings.WorkstationId : request.WorkstationId.Trim(),
            SpeakersConfigured = Math.Max(1, request.SpeakersConfigured <= 0 ? settings.SpeakersConfigured : request.SpeakersConfigured),
            NodeProfile = string.IsNullOrWhiteSpace(request.NodeProfile) ? settings.NodeProfile : request.NodeProfile.Trim()
        };
    }

    private static Difficulty ParseDifficulty(string? difficultyName, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(difficultyName)
            && Enum.TryParse(difficultyName, true, out Difficulty parsed))
        {
            return parsed;
        }

        if (!string.IsNullOrWhiteSpace(fallback)
            && Enum.TryParse(fallback, true, out parsed))
        {
            return parsed;
        }

        return TelemetryDefaults.DefaultDifficulty;
    }

    private static DateTime ParseStart(string start)
    {
        if (DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return TelemetryDefaults.Start;
    }

    private static TimeSpan ParseDuration(string durationText, string? fallbackDurationText)
    {
        if (DurationParser.TryParse(durationText, out var duration))
        {
            return duration;
        }

        if (!string.IsNullOrWhiteSpace(fallbackDurationText)
            && DurationParser.TryParse(fallbackDurationText, out var fallbackDuration))
        {
            return fallbackDuration;
        }

        return TelemetryDefaults.Duration;
    }

    private static string ResolveOutputPath(string output)
    {
        var fallback = Path.Combine(Environment.CurrentDirectory, TelemetryDefaults.OutputFileName);
        var normalized = string.IsNullOrWhiteSpace(output) ? fallback : output;

        return Path.GetFullPath(normalized, Environment.CurrentDirectory);
    }
}
