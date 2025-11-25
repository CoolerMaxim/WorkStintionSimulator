using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Core.Utilities;
using TelemetryGenerator.Generation;

namespace TelemetryGenerator.Cli;

public sealed class TelemetryGenerationRunner
{
    private readonly ITelemetryGenerationService _telemetryGenerationService;
    private readonly ILogger<TelemetryGenerationRunner> _logger;
    private readonly SimulationSettings _simulationSettings;

    public TelemetryGenerationRunner(
        ITelemetryGenerationService telemetryGenerationService,
        IOptions<SimulationSettings> simulationOptions,
        ILogger<TelemetryGenerationRunner> logger)
    {
        _telemetryGenerationService = telemetryGenerationService;
        _simulationSettings = simulationOptions.Value;
        _logger = logger;
    }

    public async Task<int> RunAsync(TelemetryGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var options = BuildTelemetryGenerationOptions(request);
        ValidateOptions(options);

        _logger.LogInformation(
            "Generating telemetry with scenario={Scenario}, duration={Duration}h, step={Step}m, workstation={Workstation}, output={Output}",
            options.Scenario,
            options.Duration.TotalHours,
            options.Step.TotalMinutes,
            options.WorkstationId,
            options.OutputPath);

        await _telemetryGenerationService.GenerateAsync(options, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    public static void Register(IServiceCollection services)
    {
        services
            .AddLogging()
            .AddTelemetryGeneration();

        services.AddOptions<SimulationSettings>();
        services.AddTransient<TelemetryGenerationRunner>();
    }

    private TelemetryGenerationOptions BuildTelemetryGenerationOptions(TelemetryGenerationRequest request)
    {
        var difficulty = ParseDifficulty(request.Difficulty);
        var start = ParseStart(string.IsNullOrWhiteSpace(request.Start) ? _simulationSettings.Start : request.Start);
        var duration = ParseDuration(string.IsNullOrWhiteSpace(request.Duration) ? _simulationSettings.Duration : request.Duration);

        var nodeProfile = string.IsNullOrWhiteSpace(request.NodeProfile)
            ? _simulationSettings.NodeProfile
            : request.NodeProfile.Trim();

        var stepMinutes = request.StepMinutes <= 0 ? _simulationSettings.StepMinutes : request.StepMinutes;

        return new TelemetryGenerationOptions
        {
            Scenario = string.IsNullOrWhiteSpace(request.Scenario) ? _simulationSettings.Scenario : request.Scenario.Trim(),
            Difficulty = difficulty,
            Start = start,
            Duration = duration,
            Step = TimeSpan.FromMinutes(Math.Max(1, stepMinutes)),
            WorkstationId = string.IsNullOrWhiteSpace(request.WorkstationId) ? _simulationSettings.WorkstationId : request.WorkstationId.Trim(),
            SpeakersConfigured = Math.Max(1, request.SpeakersConfigured <= 0 ? _simulationSettings.SpeakersConfigured : request.SpeakersConfigured),
            NodeProfile = nodeProfile,
            OutputPath = ResolveOutputPath(string.IsNullOrWhiteSpace(request.OutputPath) ? _simulationSettings.OutputPath : request.OutputPath),
            Seed = request.Seed ?? _simulationSettings.Seed
        };
    }

    private static void ValidateOptions(TelemetryGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Scenario))
        {
            throw new InvalidOperationException("Scenario cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(options.WorkstationId))
        {
            throw new InvalidOperationException("WorkstationId cannot be empty.");
        }

        if (options.Duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Duration must be greater than zero.");
        }

        if (options.Step <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Step must be greater than zero minutes.");
        }

        if (options.Step.TotalMinutes > options.Duration.TotalMinutes)
        {
            throw new InvalidOperationException("Step minutes cannot exceed total duration.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            throw new InvalidOperationException("Output path cannot be empty.");
        }
    }

    private Difficulty ParseDifficulty(string? difficultyName)
    {
        if (!string.IsNullOrWhiteSpace(difficultyName)
            && Enum.TryParse<Difficulty>(difficultyName, true, out var difficulty))
        {
            return difficulty;
        }

        if (Enum.TryParse<Difficulty>(_simulationSettings.Difficulty, true, out var simulationDifficulty))
        {
            return simulationDifficulty;
        }

        return Difficulty.Normal;
    }

    private DateTime ParseStart(string start)
    {
        if (DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return DateTime.Now;
    }

    private TimeSpan ParseDuration(string durationText)
    {
        if (DurationParser.TryParse(durationText, out var duration))
        {
            return duration;
        }

        if (DurationParser.TryParse(_simulationSettings.Duration, out var settingsDuration))
        {
            return settingsDuration;
        }

        return TimeSpan.FromHours(24);
    }

    private static string ResolveOutputPath(string output)
    {
        var fallback = Path.Combine(Environment.CurrentDirectory, TelemetryDefaults.OutputFileName);
        var normalized = string.IsNullOrWhiteSpace(output) ? fallback : output;

        return Path.GetFullPath(normalized, Environment.CurrentDirectory);
    }
}
