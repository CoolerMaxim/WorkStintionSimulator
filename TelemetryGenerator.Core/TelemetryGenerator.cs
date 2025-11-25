using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Modules;
using TelemetryGenerator.Core.Scenarios;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Core.State;
using TelemetryGenerator.Core.Telemetry;

namespace TelemetryGenerator.Core;

public sealed class TelemetryGenerator
{
    private readonly IReadOnlyList<IModule> _modules;
    private readonly IBatteryModel _batteryModel;
    private readonly AnomalyInjector _anomalyInjector;
    private readonly MaintenanceScheduler _maintenanceScheduler;

    public TelemetryGenerator(
        IReadOnlyList<IModule> modules,
        IBatteryModel batteryModel,
        AnomalyInjector anomalyInjector,
        MaintenanceScheduler maintenanceScheduler)
    {
        _modules = modules;
        _batteryModel = batteryModel;
        _anomalyInjector = anomalyInjector;
        _maintenanceScheduler = maintenanceScheduler;
    }

    public IEnumerable<TelemetrySample> Run(
        NodeConfig config,
        Scenario scenario,
        DateTime startTime,
        TimeSpan step,
        Random rnd)
    {
        if (step <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be positive.");
        }

        var profile = DifficultyProfiles.Create(scenario.Difficulty);
        var state = CreateInitialState(config, scenario, startTime);

        foreach (var phase in scenario.Phases)
        {
            var phaseStart = state.Timestamp;
            var phaseEnd = phaseStart + phase.Duration;
            phase.ConfigurePhase(state, config, profile, rnd);

            while (state.Timestamp < phaseEnd)
            {
                UpdatePowerAvailability(state, step);

                foreach (var module in _modules)
                {
                    module.Update(state, config, step, rnd);
                }

                var loadCurrent = CalculateLoadCurrent(config, state);
                _batteryModel.Update(state, config, step, loadCurrent, state.PowerStatus, state.IsChargingFromGrid);

                _anomalyInjector.Update(state, config, profile, step, rnd);
                _maintenanceScheduler.Update(state, config, profile, rnd);

                UpdateSupervisorySignals(state, config, step);

                yield return Project(state, config);

                state.Timestamp += step;
            }
        }
    }

    private static NodeState CreateInitialState(NodeConfig config, Scenario scenario, DateTime startTime)
    {
        var state = new NodeState
        {
            Timestamp = startTime,
            BatteryCapacityAhEff = config.BatteryCapacityAh,
            BatteryChargeAh = config.BatteryCapacityAh,
            BatteryVoltage = config.BatteryFullVoltage,
            BatteryStatusOk = true,
            PowerStatus = true,
            IsChargingFromGrid = true,
            SoundStatus = false,
            AmplifierStatus = false,
            AmplifierOutCurrentA = 0,
            SpeakersEffective = config.SpeakersConfigured,
            CpuTemperature = 35,
            InsideTemperature = 25,
            OutsideTemperature = 20,
            CoolingEfficiency = 0.9,
            SignalStrengthDbm = -65,
            NetworkLatencyMs = 50,
            DiskSpaceUsePercent = 30,
            DoorOpen = false,
            IsAnomaly = false,
            AnomalyType = AnomalyType.None,
            MaintenanceType = MaintenanceType.None,
            Difficulty = scenario.Difficulty,
            FirmwareVersion = config.FirmwareVersion,
            SoftwareVersion = config.SoftwareVersion,
            HardwareRevision = config.HardwareRevision,
            IsNodeOnline = true,
            HealthState = HealthState.Nominal,
            Uptime = TimeSpan.Zero,
            TotalRuntime = TimeSpan.Zero
        };

        state.RecordInitialBoot(startTime);

        return state;
    }

    private static double CalculateLoadCurrent(NodeConfig config, NodeState state)
    {
        var baseCurrent = config.McCurrentA + config.NetCurrentA;
        return baseCurrent + Math.Max(state.AmplifierOutCurrentA, 0);
    }

    private static void UpdatePowerAvailability(NodeState state, TimeSpan dt)
    {
        if (state.ForcedGridOutageRemaining > TimeSpan.Zero)
        {
            state.ForcedGridOutageRemaining = state.ForcedGridOutageRemaining > dt
                ? state.ForcedGridOutageRemaining - dt
                : TimeSpan.Zero;
            state.PowerStatus = false;
        }
        else
        {
            state.PowerStatus = true;
        }
    }

    private static TelemetrySample Project(NodeState state, NodeConfig config)
    {
        return new TelemetrySample(
            state.Timestamp,
            config.WorkStationId,
            state.PowerStatus,
            state.BatteryStatusOk,
            Math.Round(state.BatteryVoltage, 2),
            Math.Round(state.CpuTemperature, 1),
            Math.Round(state.InsideTemperature, 1),
            state.DiskSpaceUsePercent,
            state.DoorOpen,
            state.AmplifierStatus,
            Math.Round(state.AmplifierOutCurrentA, 3),
            state.SoundStatus,
            Math.Round(state.SignalStrengthDbm, 1),
            state.NetworkLatencyMs,
            config.SpeakersConfigured,
            state.IsAnomaly,
            state.AnomalyType,
            state.MaintenanceType,
            state.Uptime,
            state.TotalRuntime,
            state.RebootCount,
            state.GetRestartHistorySnapshot(),
            state.SoftwareHealth.Clone(),
            state.FirmwareVersion,
            state.SoftwareVersion,
            state.HardwareRevision,
            state.FaultCounters.CreateSnapshot(),
            state.GetIncidentLogSnapshot(),
            state.HealthState,
            state.IsNodeOnline);
    }

    private static void UpdateSupervisorySignals(NodeState state, NodeConfig config, TimeSpan step)
    {
        var wasOnline = state.IsNodeOnline;
        var hasPower = state.PowerStatus || (state.BatteryStatusOk && state.BatteryVoltage > config.BatteryCutoffVoltage - 0.2);

        if (!hasPower)
        {
            if (wasOnline)
            {
                state.IsNodeOnline = false;
                state.Uptime = TimeSpan.Zero;
                state.AddIncident("power", "Node lost power and shut down.");
            }
        }
        else
        {
            if (!wasOnline)
            {
                state.RecordRestart(state.Timestamp, "Power restored");
            }

            state.Uptime += step;
            state.TotalRuntime += step;
        }

        UpdateSoftwareHealth(state);
        UpdateHealthState(state);
    }

    private static void UpdateSoftwareHealth(NodeState state)
    {
        var previous = state.SoftwareHealth.Clone();
        var snapshot = state.SoftwareHealth;

        snapshot.FirmwareHealthy = true;
        snapshot.StorageSubsystemHealthy = state.DiskSpaceUsePercent < 95;
        snapshot.NetworkStackHealthy = state.SignalStrengthDbm > -95 && state.NetworkLatencyMs < 2500;
        snapshot.ApplicationHealthy = state.CpuTemperature < 90 && state.DiskSpaceUsePercent < 98;

        switch (state.AnomalyType)
        {
            case AnomalyType.NetControllerFailure:
            case AnomalyType.NetDegradation:
                snapshot.NetworkStackHealthy = false;
                break;
            case AnomalyType.TempSensorFailure:
                snapshot.ApplicationHealthy = false;
                break;
            case AnomalyType.SpeakersPartialFailure:
            case AnomalyType.SpeakersLineOpen:
            case AnomalyType.SpeakersLineShort:
                snapshot.ApplicationHealthy = false;
                break;
        }

        if (state.DiskSpaceUsePercent >= 98)
        {
            snapshot.StorageSubsystemHealthy = false;
        }

        if (state.CpuTemperature >= 95)
        {
            snapshot.ApplicationHealthy = false;
        }

        TrackHealthChange(previous.FirmwareHealthy, snapshot.FirmwareHealthy, state, "firmware");
        TrackHealthChange(previous.ApplicationHealthy, snapshot.ApplicationHealthy, state, "application");
        TrackHealthChange(previous.NetworkStackHealthy, snapshot.NetworkStackHealthy, state, "network");
        TrackHealthChange(previous.StorageSubsystemHealthy, snapshot.StorageSubsystemHealthy, state, "storage");
    }

    private static void TrackHealthChange(bool previous, bool current, NodeState state, string component)
    {
        if (previous == current)
        {
            return;
        }

        var status = current ? "restored" : "degraded";
        state.AddIncident("software-health", $"Component {component} {status}.");
    }

    private static void UpdateHealthState(NodeState state)
    {
        var previous = state.HealthState;

        HealthState newState;
        if (!state.IsNodeOnline)
        {
            newState = HealthState.Offline;
        }
        else if (!state.BatteryStatusOk || state.SignalStrengthDbm <= -105 || state.NetworkLatencyMs >= 4000)
        {
            newState = HealthState.Critical;
        }
        else if (state.IsAnomaly || !state.SoftwareHealth.IsHealthy || state.DiskSpaceUsePercent >= 90 || state.CpuTemperature >= 80)
        {
            newState = HealthState.Warning;
        }
        else
        {
            newState = HealthState.Nominal;
        }

        state.HealthState = newState;

        if (previous != newState)
        {
            state.AddIncident("health", $"Health state changed from {previous} to {newState}.");
        }
    }
}
