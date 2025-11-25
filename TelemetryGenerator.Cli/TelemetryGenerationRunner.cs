using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Services;
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
        var config = GenerationConfigFactory.Create(_simulationSettings, request);
        return await RunAsync(config, cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> RunAsync(GenerationConfig config, CancellationToken cancellationToken = default)
    {
        Validate(config);

        _logger.LogInformation(
            "Generating telemetry with scenario={Scenario}, duration={Duration}h, step={Step}m, workstation={Workstation}, output={Output}",
            config.Scenario,
            config.Duration.TotalHours,
            config.Step.TotalMinutes,
            config.WorkstationId,
            config.OutputPath);

        await _telemetryGenerationService.GenerateAsync(config, cancellationToken).ConfigureAwait(false);
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

    private static void Validate(GenerationConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Scenario))
        {
            throw new InvalidOperationException("Scenario cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(config.WorkstationId))
        {
            throw new InvalidOperationException("WorkstationId cannot be empty.");
        }

        if (config.Duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Duration must be greater than zero.");
        }

        if (config.Step <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Step must be greater than zero minutes.");
        }

        if (config.Step.TotalMinutes > config.Duration.TotalMinutes)
        {
            throw new InvalidOperationException("Step minutes cannot exceed total duration.");
        }

        if (string.IsNullOrWhiteSpace(config.OutputPath))
        {
            throw new InvalidOperationException("Output path cannot be empty.");
        }
    }
}
