using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Scenarios;

public static class ScenarioFactory
{
    public static Scenario CreateNormalDayScenario(Difficulty difficulty)
    {
        return new Scenario(
            "NormalDay",
            difficulty,
            new[]
            {
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    ResetState(state, config);
                    state.CoolingEfficiency = 0.9;
                    state.SoundStatus = false;
                }),
                new ScenarioPhase(TimeSpan.FromHours(10), (state, config, profile, rnd) =>
                {
                    state.CoolingEfficiency = 0.85;
                    state.SoundStatus = true;
                }),
                new ScenarioPhase(TimeSpan.FromHours(8), (state, config, profile, rnd) =>
                {
                    state.SoundStatus = false;
                    state.CoolingEfficiency = 0.95;
                })
            });
    }

    public static Scenario CreateLongPowerLossWithCutoffScenario(Difficulty difficulty, AnomalyInjector injector)
    {
        return new Scenario(
            "LongPowerLossWithCutoff",
            difficulty,
            new[]
            {
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    ResetState(state, config);
                }),
                new ScenarioPhase(TimeSpan.FromHours(12), (state, config, profile, rnd) =>
                {
                    state.ForcedGridOutageRemaining = TimeSpan.FromHours(12);
                    state.SoundStatus = false;
                    injector.ForceAnomaly(AnomalyType.BatteryCutoff, state, config, state.Difficulty, rnd);
                }),
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    state.PlannedMaintenance.Enqueue((MaintenanceType.BatteryReplacement, state.Timestamp + TimeSpan.FromHours(1)));
                    state.SoundStatus = false;
                })
            });
    }

    public static Scenario CreateSpeakersDegradationWithRepairScenario(Difficulty difficulty, AnomalyInjector injector)
    {
        return new Scenario(
            "SpeakersDegradationWithRepair",
            difficulty,
            new[]
            {
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    ResetState(state, config);
                }),
                new ScenarioPhase(TimeSpan.FromHours(20), (state, config, profile, rnd) =>
                {
                    state.SpeakersEffective = Math.Max(1, config.SpeakersConfigured - 2);
                    injector.ForceAnomaly(AnomalyType.SpeakersPartialFailure, state, config, state.Difficulty, rnd);
                }),
                new ScenarioPhase(TimeSpan.FromHours(4), (state, config, profile, rnd) =>
                {
                    state.PlannedMaintenance.Enqueue((MaintenanceType.SpeakersRepair, state.Timestamp + TimeSpan.FromHours(1)));
                })
            });
    }

    public static Scenario CreateNetDegradationToFailureScenario(Difficulty difficulty, AnomalyInjector injector)
    {
        return new Scenario(
            "NetDegradationToFailure",
            difficulty,
            new[]
            {
                new ScenarioPhase(TimeSpan.FromHours(8), (state, config, profile, rnd) =>
                {
                    ResetState(state, config);
                }),
                new ScenarioPhase(TimeSpan.FromHours(10), (state, config, profile, rnd) =>
                {
                    injector.ForceAnomaly(AnomalyType.NetDegradation, state, config, state.Difficulty, rnd);
                }),
                new ScenarioPhase(TimeSpan.FromHours(4), (state, config, profile, rnd) =>
                {
                    injector.ForceAnomaly(AnomalyType.NetControllerFailure, state, config, state.Difficulty, rnd);
                }),
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    state.PlannedMaintenance.Enqueue((MaintenanceType.CoolingService, state.Timestamp + TimeSpan.FromHours(2)));
                })
            });
    }

    public static Scenario CreateCoolingDegradationWithServiceScenario(Difficulty difficulty, AnomalyInjector injector)
    {
        return new Scenario(
            "CoolingDegradationWithService",
            difficulty,
            new[]
            {
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    ResetState(state, config);
                }),
                new ScenarioPhase(TimeSpan.FromHours(12), (state, config, profile, rnd) =>
                {
                    state.CoolingEfficiency = 0.5;
                    injector.ForceAnomaly(AnomalyType.CoolingDegradation, state, config, state.Difficulty, rnd);
                }),
                new ScenarioPhase(TimeSpan.FromHours(6), (state, config, profile, rnd) =>
                {
                    state.PlannedMaintenance.Enqueue((MaintenanceType.CoolingService, state.Timestamp + TimeSpan.FromHours(1)));
                })
            });
    }

    private static void ResetState(NodeState state, NodeConfig config)
    {
        state.CoolingEfficiency = 0.9;
        state.SpeakersEffective = config.SpeakersConfigured;
        state.SoundStatus = false;
        state.ForcedGridOutageRemaining = TimeSpan.Zero;
        state.IsAnomaly = false;
        state.AnomalyType = AnomalyType.None;
        while (state.PlannedMaintenance.Count > 0)
        {
            state.PlannedMaintenance.Dequeue();
        }
    }
}
