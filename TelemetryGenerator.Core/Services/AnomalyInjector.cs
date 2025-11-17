using System.Linq;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Services;

public sealed class AnomalyInjector
{
    private sealed record ActiveAnomaly(AnomalyType Type, TimeSpan Remaining, Action<NodeState> Restore);

    private readonly List<ActiveAnomaly> _active = new();

    public void Update(NodeState state, NodeConfig config, DifficultyProfile profile, TimeSpan dt, Random rnd)
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

        if (_active.Count < profile.MaxParallelAnomalies && rnd.NextDouble() < profile.AnomalyRate * dt.TotalHours)
        {
            var options = Enum.GetValues<AnomalyType>().Where(t => t != AnomalyType.None).ToArray();
            var randomType = options[rnd.Next(options.Length)];
            ForceAnomaly(randomType, state, config, rnd);
        }

        state.IsAnomaly = _active.Count > 0;
        state.AnomalyType = _active.Count > 0 ? _active[^1].Type : AnomalyType.None;
    }

    public void ForceAnomaly(AnomalyType type, NodeState state, NodeConfig config, Random rnd)
    {
        var anomaly = type switch
        {
            AnomalyType.BatteryCutoff => CreateBatteryCutoff(state, config, rnd),
            AnomalyType.NetControllerFailure => CreateNetControllerFailure(state, rnd),
            AnomalyType.SpeakersPartialFailure => CreateSpeakersPartialFailure(state, rnd),
            AnomalyType.SpeakersLineOpen => CreateSpeakersLineOpen(state, rnd),
            AnomalyType.SpeakersLineShort => CreateSpeakersLineShort(state, rnd),
            AnomalyType.BatteryDegradation => CreateBatteryDegradation(state, config, rnd),
            AnomalyType.NetDegradation => CreateNetDegradation(state, rnd),
            AnomalyType.CoolingDegradation => CreateCoolingDegradation(state, rnd),
            AnomalyType.TempSensorFailure => CreateTempSensorFailure(state, rnd),
            _ => null
        };

        if (anomaly is not null)
        {
            _active.Add(anomaly);
            state.IsAnomaly = true;
            state.AnomalyType = type;
            state.FaultCounters.RecordAnomaly(type);
            state.AddIncident("anomaly", $"Anomaly {type} detected.");
        }
    }

    private ActiveAnomaly CreateBatteryCutoff(NodeState state, NodeConfig config, Random rnd)
    {
        var previousStatus = state.BatteryStatusOk;
        var previousVoltage = state.BatteryVoltage;
        var previousCharge = state.BatteryChargeAh;
        state.BatteryChargeAh = Math.Min(state.BatteryChargeAh, state.BatteryCapacityAhEff * 0.05);
        state.BatteryVoltage = config.BatteryCutoffVoltage - 0.2;
        state.BatteryStatusOk = false;
        return new ActiveAnomaly(
            AnomalyType.BatteryCutoff,
            TimeSpan.FromMinutes(rnd.Next(30, 180)),
            s =>
            {
                s.BatteryStatusOk = previousStatus;
                s.BatteryVoltage = Math.Max(s.BatteryVoltage, previousVoltage);
                s.BatteryChargeAh = Math.Max(s.BatteryChargeAh, previousCharge * 0.5);
            });
    }

    private ActiveAnomaly CreateNetControllerFailure(NodeState state, Random rnd)
    {
        var previousLatency = state.NetworkLatencyMs;
        var previousSignal = state.SignalStrengthDbm;
        return new ActiveAnomaly(
            AnomalyType.NetControllerFailure,
            TimeSpan.FromMinutes(rnd.Next(60, 240)),
            s =>
            {
                s.NetworkLatencyMs = previousLatency;
                s.SignalStrengthDbm = previousSignal;
            });
    }

    private ActiveAnomaly CreateSpeakersPartialFailure(NodeState state, Random rnd)
    {
        var previous = state.SpeakersEffective;
        state.SpeakersEffective = Math.Max(1, (int)Math.Round(state.SpeakersEffective * 0.5));
        return new ActiveAnomaly(
            AnomalyType.SpeakersPartialFailure,
            TimeSpan.FromMinutes(rnd.Next(120, 360)),
            s => s.SpeakersEffective = previous);
    }

    private ActiveAnomaly CreateSpeakersLineOpen(NodeState state, Random rnd)
    {
        var previous = state.SpeakersEffective;
        state.SpeakersEffective = 0;
        return new ActiveAnomaly(
            AnomalyType.SpeakersLineOpen,
            TimeSpan.FromMinutes(rnd.Next(60, 240)),
            s => s.SpeakersEffective = previous);
    }

    private ActiveAnomaly CreateSpeakersLineShort(NodeState state, Random rnd)
    {
        var previousSpeakers = state.SpeakersEffective;
        var previousSound = state.SoundStatus;
        state.SpeakersEffective = 0;
        state.SoundStatus = false;
        return new ActiveAnomaly(
            AnomalyType.SpeakersLineShort,
            TimeSpan.FromMinutes(rnd.Next(20, 120)),
            s =>
            {
                s.SpeakersEffective = previousSpeakers;
                s.SoundStatus = previousSound;
            });
    }

    private ActiveAnomaly CreateBatteryDegradation(NodeState state, NodeConfig config, Random rnd)
    {
        var previousCapacity = state.BatteryCapacityAhEff;
        state.BatteryCapacityAhEff = Math.Max(config.BatteryCapacityAh * 0.4, state.BatteryCapacityAhEff * 0.8);
        return new ActiveAnomaly(
            AnomalyType.BatteryDegradation,
            TimeSpan.FromMinutes(rnd.Next(240, 720)),
            s => s.BatteryCapacityAhEff = Math.Max(s.BatteryCapacityAhEff, previousCapacity));
    }

    private ActiveAnomaly CreateNetDegradation(NodeState state, Random rnd)
    {
        var previous = state.AnomalyType;
        state.AnomalyType = AnomalyType.NetDegradation;
        return new ActiveAnomaly(
            AnomalyType.NetDegradation,
            TimeSpan.FromMinutes(rnd.Next(120, 360)),
            s =>
            {
                if (s.AnomalyType == AnomalyType.NetDegradation)
                {
                    s.AnomalyType = previous;
                }
            });
    }

    private ActiveAnomaly CreateCoolingDegradation(NodeState state, Random rnd)
    {
        var previous = state.CoolingEfficiency;
        state.CoolingEfficiency = Math.Max(0.2, state.CoolingEfficiency - 0.4);
        return new ActiveAnomaly(
            AnomalyType.CoolingDegradation,
            TimeSpan.FromMinutes(rnd.Next(180, 600)),
            s => s.CoolingEfficiency = Math.Max(s.CoolingEfficiency, previous));
    }

    private ActiveAnomaly CreateTempSensorFailure(NodeState state, Random rnd)
    {
        var previousInside = state.InsideTemperature;
        var previousOutside = state.OutsideTemperature;
        state.InsideTemperature += rnd.NextDouble() * 30 - 15;
        state.OutsideTemperature += rnd.NextDouble() * 30 - 15;
        return new ActiveAnomaly(
            AnomalyType.TempSensorFailure,
            TimeSpan.FromMinutes(rnd.Next(60, 180)),
            s =>
            {
                s.InsideTemperature = previousInside;
                s.OutsideTemperature = previousOutside;
            });
    }
}
