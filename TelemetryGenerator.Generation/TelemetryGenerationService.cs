using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using TelemetryGenerator.Core;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Modules;
using TelemetryGenerator.Core.Scenarios;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Core.Telemetry;

namespace TelemetryGenerator.Generation;

public sealed class TelemetryGenerationService : ITelemetryGenerationService
{
    private readonly IRandomFactory _randomFactory;
    private readonly IAnomalyInjectorFactory _anomalyInjectorFactory;
    private readonly IBatteryModelFactory _batteryModelFactory;
    private readonly ITemperatureModelFactory _temperatureModelFactory;
    private readonly IMaintenanceSchedulerFactory _maintenanceSchedulerFactory;
    private readonly IScenarioRegistry _scenarioRegistry;
    private readonly IModuleProvider _moduleProvider;

    public TelemetryGenerationService(
        IRandomFactory randomFactory,
        IAnomalyInjectorFactory anomalyInjectorFactory,
        IBatteryModelFactory batteryModelFactory,
        ITemperatureModelFactory temperatureModelFactory,
        IMaintenanceSchedulerFactory maintenanceSchedulerFactory,
        IScenarioRegistry scenarioRegistry,
        IModuleProvider moduleProvider)
    {
        _randomFactory = randomFactory;
        _anomalyInjectorFactory = anomalyInjectorFactory;
        _batteryModelFactory = batteryModelFactory;
        _temperatureModelFactory = temperatureModelFactory;
        _maintenanceSchedulerFactory = maintenanceSchedulerFactory;
        _scenarioRegistry = scenarioRegistry;
        _moduleProvider = moduleProvider;
    }

    public async Task GenerateAsync(GenerationConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        Validate(config);

        var rnd = _randomFactory.Create(config.Seed);
        var difficulty = config.Difficulty;
        var nodeProfile = config.NodeProfile ?? "randomized";
        var nodeConfig = CreateNodeConfig(nodeProfile, config.WorkstationId, config.SpeakersConfigured, difficulty, rnd);

        var anomalyInjector = _anomalyInjectorFactory.Create();
        var maintenanceScheduler = _maintenanceSchedulerFactory.Create();
        var batteryModel = _batteryModelFactory.Create();
        var temperatureModel = _temperatureModelFactory.Create();
        var profile = DifficultyProfiles.Create(difficulty);
        var modules = _moduleProvider.CreateModules(profile, temperatureModel).ToList();

        var generator = new TelemetryGenerator.Core.TelemetryGenerator(
            modules,
            batteryModel,
            anomalyInjector,
            maintenanceScheduler);

        var scenario = _scenarioRegistry.Resolve(config.Scenario, difficulty, anomalyInjector);
        var endTime = config.Start + config.Duration;
        var samples = generator
            .Run(nodeConfig, scenario, config.Start, config.Step, rnd)
            .TakeWhile(sample => sample.Timestamp < endTime);

        await WriteCsvAsync(config.OutputPath, samples, cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(GenerationConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Scenario))
        {
            throw new ArgumentException("Scenario is required.", nameof(config));
        }

        if (config.Step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "Step must be positive.");
        }

        if (config.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "Duration must be positive.");
        }

        if (string.IsNullOrWhiteSpace(config.WorkstationId))
        {
            throw new ArgumentException("WorkstationId is required.", nameof(config));
        }

        if (config.SpeakersConfigured < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "SpeakersConfigured must be positive.");
        }

        if (string.IsNullOrWhiteSpace(config.OutputPath))
        {
            throw new ArgumentException("OutputPath is required.", nameof(config));
        }

        if (config.Seed is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config), "Seed must be non-negative when specified.");
        }
    }

    private static NodeConfig CreateNodeConfig(string profile, string workStationId, int speakersConfigured, Difficulty difficulty, Random rnd)
    {
        profile ??= string.Empty;
        return profile.ToLowerInvariant() switch
        {
            "fixed" or "baseline" => NodeConfig.CreateDefault(workStationId, speakersConfigured),
            "random" or "randomized" or "mixed" => NodeConfigurationFactory.CreateRandomized(workStationId, speakersConfigured, difficulty, rnd),
            _ => NodeConfigurationFactory.CreateRandomized(workStationId, speakersConfigured, difficulty, rnd)
        };
    }

    private static async Task WriteCsvAsync(string outputPath, IEnumerable<TelemetrySample> samples, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = new StreamWriter(outputPath);
        await writer.WriteLineAsync(
            "Timestamp,WorkStationId,PowerStatus,BatteryStatus,BatteryVoltage,CpuTemperature,InsideTemperature,DiskSpaceUse,DoorOpenStatus,AmplifierStatus,AmplifierOutPower,SoundStatus,SignalStrength,NetworkLatency,SpeakersConfigured,IsAnomaly,AnomalyType,MaintenanceType,NodeUptimeMinutes,TotalRuntimeHours,RestartCount,RecentRestarts,SoftwareHealth,FirmwareVersion,SoftwareVersion,HardwareRevision,FaultCounters,HealthState,IncidentLog,NodeOnline").ConfigureAwait(false);

        foreach (var sample in samples)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync(string.Join(',', new[]
            {
                sample.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                sample.WorkStationId,
                sample.PowerStatus ? "true" : "false",
                sample.BatteryStatus ? "true" : "false",
                sample.BatteryVoltage.ToString("F2", CultureInfo.InvariantCulture),
                sample.CpuTemperature.ToString("F1", CultureInfo.InvariantCulture),
                sample.InsideTemperature.ToString("F1", CultureInfo.InvariantCulture),
                sample.DiskSpaceUse.ToString(CultureInfo.InvariantCulture),
                sample.DoorOpenStatus ? "true" : "false",
                sample.AmplifierStatus ? "true" : "false",
                sample.AmplifierOutPower.ToString("F3", CultureInfo.InvariantCulture),
                sample.SoundStatus ? "true" : "false",
                sample.SignalStrength.ToString("F1", CultureInfo.InvariantCulture),
                sample.NetworkLatency.ToString(CultureInfo.InvariantCulture),
                sample.SpeakersConfigured.ToString(CultureInfo.InvariantCulture),
                sample.IsAnomaly ? "true" : "false",
                sample.AnomalyType.ToString(),
                sample.MaintenanceType.ToString(),
                sample.NodeUptime.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture),
                sample.TotalRuntime.TotalHours.ToString("F1", CultureInfo.InvariantCulture),
                sample.RestartCount.ToString(CultureInfo.InvariantCulture),
                Quote(string.Join('|', sample.RestartHistory.Select(r => r.ToString("O", CultureInfo.InvariantCulture)))),
                Quote(sample.SoftwareHealth.ToSummaryString()),
                Quote(sample.FirmwareVersion),
                Quote(sample.SoftwareVersion),
                Quote(sample.HardwareRevision),
                Quote(sample.FaultCounters.ToSummaryString()),
                sample.HealthState.ToString(),
                Quote(string.Join('|', sample.IncidentLog.Select(i => i.ToSummaryString()))),
                sample.IsNodeOnline ? "true" : "false"
            })).ConfigureAwait(false);
        }
    }

    private static string Quote(string value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

public static class TelemetryGenerationServiceCollectionExtensions
{
    public static IServiceCollection AddTelemetryGeneration(this IServiceCollection services)
    {
        services.AddSingleton<IRandomFactory, DefaultRandomFactory>();
        services.AddSingleton<IAnomalyInjectorFactory, DefaultAnomalyInjectorFactory>();
        services.AddSingleton<IBatteryModelFactory, DefaultBatteryModelFactory>();
        services.AddSingleton<ITemperatureModelFactory, DefaultTemperatureModelFactory>();
        services.AddSingleton<IMaintenanceSchedulerFactory, DefaultMaintenanceSchedulerFactory>();
        services.AddSingleton<IScenarioRegistry>(_ => CreateScenarioRegistry());
        services.AddSingleton<IModuleProvider, DefaultModuleProvider>();
        services.AddSingleton<ITelemetryGenerationService, TelemetryGenerationService>();
        return services;
    }

    private static ScenarioRegistry CreateScenarioRegistry()
    {
        var registry = new ScenarioRegistry();
        Register(registry, (difficulty, _) => ScenarioFactory.CreateNormalDayScenario(difficulty), "normal", "normalday", "normal-day");
        Register(registry, ScenarioFactory.CreateLongPowerLossWithCutoffScenario, "powerloss", "longpowerloss");
        Register(registry, ScenarioFactory.CreateSpeakersDegradationWithRepairScenario, "speakers", "speakers-degradation");
        Register(registry, ScenarioFactory.CreateNetDegradationToFailureScenario, "net", "net-failure");
        Register(registry, ScenarioFactory.CreateCoolingDegradationWithServiceScenario, "cooling", "cooling-service");
        return registry;
    }

    private static void Register(
        ScenarioRegistry registry,
        Func<Difficulty, AnomalyInjector, Scenario> factory,
        params string[] names)
    {
        foreach (var name in names)
        {
            registry.Register(name, factory);
        }
    }
}
