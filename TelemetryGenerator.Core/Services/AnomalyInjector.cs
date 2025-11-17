using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Services;

public sealed class AnomalyInjector
{
    private sealed record ActiveAnomaly(AnomalyType Type, TimeSpan Remaining, Action<NodeState> Restore);
    private sealed record ScheduledAnomaly(AnomalyType Type, TimeSpan Remaining);

    private readonly List<ActiveAnomaly> _active = new();
    private readonly List<ScheduledAnomaly> _scheduled = new();
    private readonly AnomalyConfiguration _configuration;

    public AnomalyInjector(AnomalyConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Update(NodeState state, NodeConfig config, DifficultyProfile profile, TimeSpan dt, Random rnd)
    {
        ResolveActiveAnomalies(state, dt);
        ProcessScheduledAnomalies(state, config, profile, dt, rnd);

        var availableSlots = profile.MaxParallelAnomalies - _active.Count;

        if (availableSlots > 0 && rnd.NextDouble() < profile.AnomalyRate * dt.TotalHours)
        {
            var nextType = _configuration.PickWeighted(rnd, Enum.GetValues<AnomalyType>());
            if (nextType.HasValue)
            {
                ForceAnomaly(nextType.Value, state, config, state.Difficulty, rnd);
            }
        }

        availableSlots = profile.MaxParallelAnomalies - _active.Count;

        if (availableSlots > 0 && _active.Count > 0 && rnd.NextDouble() < _configuration.CombinationProbability)
        {
            var occupiedTypes = _active.Select(a => a.Type).ToHashSet();
            var candidate = _configuration.PickWeighted(rnd, Enum.GetValues<AnomalyType>().Except(occupiedTypes));
            if (candidate.HasValue)
            {
                ForceAnomaly(candidate.Value, state, config, state.Difficulty, rnd);
            }
        }

        state.IsAnomaly = _active.Count > 0;
        state.AnomalyType = _active.Count > 0 ? _active[^1].Type : AnomalyType.None;
    }

    public void ForceAnomaly(AnomalyType type, NodeState state, NodeConfig config, Difficulty difficulty, Random rnd)
    {
        var duration = _configuration.GetDuration(type, difficulty, rnd);
        var anomaly = type switch
        {
            AnomalyType.BatteryCutoff => CreateBatteryCutoff(state, config, duration),
            AnomalyType.NetControllerFailure => CreateNetControllerFailure(state, duration),
            AnomalyType.SpeakersPartialFailure => CreateSpeakersPartialFailure(state, duration),
            AnomalyType.SpeakersLineOpen => CreateSpeakersLineOpen(state, duration),
            AnomalyType.SpeakersLineShort => CreateSpeakersLineShort(state, duration),
            AnomalyType.BatteryDegradation => CreateBatteryDegradation(state, config, duration),
            AnomalyType.NetDegradation => CreateNetDegradation(state, duration),
            AnomalyType.CoolingDegradation => CreateCoolingDegradation(state, duration),
            AnomalyType.TempSensorFailure => CreateTempSensorFailure(state, duration, rnd),
            _ => null
        };

        if (anomaly is not null)
        {
            _active.Add(anomaly);
            state.IsAnomaly = true;
            state.AnomalyType = type;
            state.FaultCounters.RecordAnomaly(type);
            state.AddIncident("anomaly", $"Anomaly {type} detected.");

            var dependent = _configuration.Dependencies.Where(d => d.Trigger == type);
            foreach (var dependency in dependent)
            {
                foreach (var followUp in dependency.FollowUps)
                {
                    var delay = dependency.DelayRange.Sample(rnd);
                    _scheduled.Add(new ScheduledAnomaly(followUp, delay));
                }
            }
        }
    }

    private void ResolveActiveAnomalies(NodeState state, TimeSpan dt)
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var anomaly = _active[i];
            var updated = anomaly with { Remaining = anomaly.Remaining - dt };
            if (updated.Remaining <= TimeSpan.Zero)
            {
                anomaly.Restore(state);
                _active.RemoveAt(i);
                state.AddIncident("anomaly", $"Anomaly {anomaly.Type} resolved.");
            }
            else
            {
                _active[i] = updated;
            }
        }
    }

    private void ProcessScheduledAnomalies(NodeState state, NodeConfig config, DifficultyProfile profile, TimeSpan dt, Random rnd)
    {
        for (var i = _scheduled.Count - 1; i >= 0; i--)
        {
            var scheduled = _scheduled[i] with { Remaining = _scheduled[i].Remaining - dt };
            if (scheduled.Remaining <= TimeSpan.Zero)
            {
                _scheduled.RemoveAt(i);
                if (_active.Count < profile.MaxParallelAnomalies)
                {
                    ForceAnomaly(scheduled.Type, state, config, state.Difficulty, rnd);
                }
            }
            else
            {
                _scheduled[i] = scheduled;
            }
        }
    }

    private ActiveAnomaly CreateBatteryCutoff(NodeState state, NodeConfig config, TimeSpan duration)
    {
        var previousStatus = state.BatteryStatusOk;
        var previousVoltage = state.BatteryVoltage;
        var previousCharge = state.BatteryChargeAh;
        state.BatteryChargeAh = Math.Min(state.BatteryChargeAh, state.BatteryCapacityAhEff * 0.05);
        state.BatteryVoltage = config.BatteryCutoffVoltage - 0.2;
        state.BatteryStatusOk = false;
        return new ActiveAnomaly(
            AnomalyType.BatteryCutoff,
            duration,
            s =>
            {
                s.BatteryStatusOk = previousStatus;
                s.BatteryVoltage = Math.Max(s.BatteryVoltage, previousVoltage);
                s.BatteryChargeAh = Math.Max(s.BatteryChargeAh, previousCharge * 0.5);
            });
    }

    private ActiveAnomaly CreateNetControllerFailure(NodeState state, TimeSpan duration)
    {
        var previousLatency = state.NetworkLatencyMs;
        var previousSignal = state.SignalStrengthDbm;
        return new ActiveAnomaly(
            AnomalyType.NetControllerFailure,
            duration,
            s =>
            {
                s.NetworkLatencyMs = previousLatency;
                s.SignalStrengthDbm = previousSignal;
            });
    }

    private ActiveAnomaly CreateSpeakersPartialFailure(NodeState state, TimeSpan duration)
    {
        var previous = state.SpeakersEffective;
        state.SpeakersEffective = Math.Max(1, (int)Math.Round(state.SpeakersEffective * 0.5));
        return new ActiveAnomaly(
            AnomalyType.SpeakersPartialFailure,
            duration,
            s => s.SpeakersEffective = previous);
    }

    private ActiveAnomaly CreateSpeakersLineOpen(NodeState state, TimeSpan duration)
    {
        var previous = state.SpeakersEffective;
        state.SpeakersEffective = 0;
        return new ActiveAnomaly(
            AnomalyType.SpeakersLineOpen,
            duration,
            s => s.SpeakersEffective = previous);
    }

    private ActiveAnomaly CreateSpeakersLineShort(NodeState state, TimeSpan duration)
    {
        var previousSpeakers = state.SpeakersEffective;
        var previousSound = state.SoundStatus;
        state.SpeakersEffective = 0;
        state.SoundStatus = false;
        return new ActiveAnomaly(
            AnomalyType.SpeakersLineShort,
            duration,
            s =>
            {
                s.SpeakersEffective = previousSpeakers;
                s.SoundStatus = previousSound;
            });
    }

    private ActiveAnomaly CreateBatteryDegradation(NodeState state, NodeConfig config, TimeSpan duration)
    {
        var previousCapacity = state.BatteryCapacityAhEff;
        state.BatteryCapacityAhEff = Math.Max(config.BatteryCapacityAh * 0.4, state.BatteryCapacityAhEff * 0.8);
        return new ActiveAnomaly(
            AnomalyType.BatteryDegradation,
            duration,
            s => s.BatteryCapacityAhEff = Math.Max(s.BatteryCapacityAhEff, previousCapacity));
    }

    private ActiveAnomaly CreateNetDegradation(NodeState state, TimeSpan duration)
    {
        var previous = state.AnomalyType;
        state.AnomalyType = AnomalyType.NetDegradation;
        return new ActiveAnomaly(
            AnomalyType.NetDegradation,
            duration,
            s =>
            {
                if (s.AnomalyType == AnomalyType.NetDegradation)
                {
                    s.AnomalyType = previous;
                }
            });
    }

    private ActiveAnomaly CreateCoolingDegradation(NodeState state, TimeSpan duration)
    {
        var previous = state.CoolingEfficiency;
        state.CoolingEfficiency = Math.Max(0.2, state.CoolingEfficiency - 0.4);
        return new ActiveAnomaly(
            AnomalyType.CoolingDegradation,
            duration,
            s => s.CoolingEfficiency = Math.Max(s.CoolingEfficiency, previous));
    }

    private ActiveAnomaly CreateTempSensorFailure(NodeState state, TimeSpan duration, Random rnd)
    {
        var previousInside = state.InsideTemperature;
        var previousOutside = state.OutsideTemperature;
        state.InsideTemperature += rnd.NextDouble() * 30 - 15;
        state.OutsideTemperature += rnd.NextDouble() * 30 - 15;
        return new ActiveAnomaly(
            AnomalyType.TempSensorFailure,
            duration,
            s =>
            {
                s.InsideTemperature = previousInside;
                s.OutsideTemperature = previousOutside;
            });
    }
}
