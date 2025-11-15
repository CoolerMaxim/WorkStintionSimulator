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
    private readonly BatteryModel _batteryModel;
    private readonly TemperatureModel _temperatureModel;
    private readonly AnomalyInjector _anomalyInjector;
    private readonly MaintenanceScheduler _maintenanceScheduler;

    public TelemetryGenerator(
        IReadOnlyList<IModule> modules,
        BatteryModel batteryModel,
        TemperatureModel temperatureModel,
        AnomalyInjector anomalyInjector,
        MaintenanceScheduler maintenanceScheduler)
    {
        _modules = modules;
        _batteryModel = batteryModel;
        _temperatureModel = temperatureModel;
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
                state.OutsideTemperature = _temperatureModel.GetOutsideTemperature(state.Timestamp, rnd);

                foreach (var module in _modules)
                {
                    module.Update(state, config, step, rnd);
                }

                var loadCurrent = CalculateLoadCurrent(config, state);
                _batteryModel.Update(state, config, step, loadCurrent, state.PowerStatus, state.ChargeMode);

                var loadFactor = Math.Clamp(loadCurrent / (config.McCurrentA + config.NetCurrentA + config.SpeakerCurrentA * Math.Max(1, config.SpeakersConfigured)), 0, 1);
                _temperatureModel.UpdateInsideTemperature(state, loadFactor, step, rnd);

                _anomalyInjector.Update(state, config, profile, step, rnd);
                _maintenanceScheduler.Update(state, config, profile, rnd);

                yield return Project(state, config);

                state.Timestamp += step;
            }
        }
    }

    private static NodeState CreateInitialState(NodeConfig config, Scenario scenario, DateTime startTime)
    {
        return new NodeState
        {
            Timestamp = startTime,
            BatteryCapacityAhEff = config.BatteryCapacityAh,
            BatteryChargeAh = config.BatteryCapacityAh,
            BatteryVoltage = config.BatteryFullVoltage,
            BatteryStatusOk = true,
            PowerStatus = true,
            ChargeMode = true,
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
            Difficulty = scenario.Difficulty
        };
    }

    private static double CalculateLoadCurrent(NodeConfig config, NodeState state)
    {
        var baseCurrent = config.McCurrentA + config.NetCurrentA;
        return baseCurrent + Math.Max(state.AmplifierOutCurrentA, 0);
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
            (int)Math.Round(state.InsideTemperature),
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
            state.MaintenanceType);
    }
}
