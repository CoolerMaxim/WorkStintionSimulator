using System.Globalization;
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
    public async Task GenerateAsync(TelemetryGenerationOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        Validate(options);

        var rnd = new Random();
        var difficulty = options.Difficulty;
        var nodeProfile = options.NodeProfile ?? "randomized";
        var config = CreateNodeConfig(nodeProfile, options.WorkstationId, options.SpeakersConfigured, difficulty, rnd);

        var anomalyConfiguration = AnomalyConfigurationFactory.CreateDefault();
        var anomalyInjector = new AnomalyInjector(anomalyConfiguration);
        var maintenanceScheduler = new MaintenanceScheduler();
        var batteryModel = new BatteryModel();
        var temperatureModel = new TemperatureModel();
        var profile = DifficultyProfiles.Create(difficulty);
        var modules = new List<IModule>
        {
            new PowerSupplyModule(),
            new MicroControllerModule(),
            new AmplifierModule(),
            new EnvironmentModule(temperatureModel),
            new NetworkControllerModule(new NetworkModel(), profile)
        };

        var generator = new TelemetryGenerator.Core.TelemetryGenerator(
            modules,
            batteryModel,
            anomalyInjector,
            maintenanceScheduler);

        var scenario = CreateScenario(options.Scenario, difficulty, anomalyInjector);
        var endTime = options.Start + options.Duration;
        var samples = generator
            .Run(config, scenario, options.Start, options.Step, rnd)
            .TakeWhile(sample => sample.Timestamp < endTime);

        await WriteCsvAsync(options.OutputPath, samples, cancellationToken).ConfigureAwait(false);
    }

    private static void Validate(TelemetryGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Scenario))
        {
            throw new ArgumentException("Scenario is required.", nameof(options));
        }

        if (options.Step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Step must be positive.");
        }

        if (options.Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Duration must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.WorkstationId))
        {
            throw new ArgumentException("WorkstationId is required.", nameof(options));
        }

        if (options.SpeakersConfigured < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "SpeakersConfigured must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            throw new ArgumentException("OutputPath is required.", nameof(options));
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

    private static Scenario CreateScenario(string name, Difficulty difficulty, AnomalyInjector injector)
    {
        return name.ToLowerInvariant() switch
        {
            "normal" or "normalday" or "normal-day" => ScenarioFactory.CreateNormalDayScenario(difficulty),
            "powerloss" or "longpowerloss" => ScenarioFactory.CreateLongPowerLossWithCutoffScenario(difficulty, injector),
            "speakers" or "speakers-degradation" => ScenarioFactory.CreateSpeakersDegradationWithRepairScenario(difficulty, injector),
            "net" or "net-failure" => ScenarioFactory.CreateNetDegradationToFailureScenario(difficulty, injector),
            "cooling" or "cooling-service" => ScenarioFactory.CreateCoolingDegradationWithServiceScenario(difficulty, injector),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown scenario.")
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
            "Timestamp,WorkStationId,PowerStatus,BatteryStatus,BatteryVoltage,CpuTemperature,Temperature,DiskSpaceUse,DoorOpenStatus,AmplifierStatus,AmplifierOutPower,SoundStatus,SignalStrength,NetworkLatency,SpeakersConfigured,IsAnomaly,AnomalyType,MaintenanceType,NodeUptimeMinutes,TotalRuntimeHours,RestartCount,RecentRestarts,SoftwareHealth,FirmwareVersion,SoftwareVersion,HardwareRevision,FaultCounters,HealthState,IncidentLog,NodeOnline").ConfigureAwait(false);

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
                sample.Temperature.ToString(CultureInfo.InvariantCulture),
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
        services.AddSingleton<ITelemetryGenerationService, TelemetryGenerationService>();
        return services;
    }
}
