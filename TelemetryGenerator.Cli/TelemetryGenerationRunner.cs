using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Utilities;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Generation;

namespace TelemetryGenerator.Cli;

public sealed class TelemetryGenerationRunner
{
    private readonly ITelemetryGenerationService _telemetryGenerationService;
    private readonly ILogger<TelemetryGenerationRunner> _logger;

    public TelemetryGenerationRunner(ITelemetryGenerationService telemetryGenerationService, ILogger<TelemetryGenerationRunner> logger)
    {
        _telemetryGenerationService = telemetryGenerationService;
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
            .AddTelemetryGeneration()
            .AddTransient<TelemetryGenerationRunner>();
    }

    private static TelemetryGenerationOptions BuildTelemetryGenerationOptions(TelemetryGenerationRequest request)
    {
        if (!Enum.TryParse<Difficulty>(request.Difficulty, true, out var difficulty))
        {
            difficulty = TelemetryDefaults.DefaultDifficulty;
        }

        var start = ParseStart(request.Start);
        if (!DurationParser.TryParse(request.Duration, out var duration))
        {
            duration = TelemetryDefaults.Duration;
        }

        var nodeProfile = string.IsNullOrWhiteSpace(request.NodeProfile)
            ? TelemetryDefaults.NodeProfile
            : request.NodeProfile.Trim();

        var stepMinutes = request.StepMinutes <= 0 ? TelemetryDefaults.StepMinutes : request.StepMinutes;

        return new TelemetryGenerationOptions
        {
            Scenario = string.IsNullOrWhiteSpace(request.Scenario) ? TelemetryDefaults.Scenario : request.Scenario.Trim(),
            Difficulty = difficulty,
            Start = start,
            Duration = duration,
            Step = TimeSpan.FromMinutes(Math.Max(1, stepMinutes)),
            WorkstationId = string.IsNullOrWhiteSpace(request.WorkstationId) ? TelemetryDefaults.WorkstationId : request.WorkstationId.Trim(),
            SpeakersConfigured = Math.Max(1, request.SpeakersConfigured <= 0 ? TelemetryDefaults.SpeakersConfigured : request.SpeakersConfigured),
            NodeProfile = nodeProfile,
            OutputPath = ResolveOutputPath(request.OutputPath),
            Seed = request.Seed
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

    private static DateTime ParseStart(string start)
    {
        if (DateTime.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        return TelemetryDefaults.Start;
    }

    private static string ResolveOutputPath(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return TelemetryDefaults.BuildOutputPath();
        }

        return Path.GetFullPath(output, Environment.CurrentDirectory);
    }
}
