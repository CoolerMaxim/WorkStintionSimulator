using System;
using System.Collections.Generic;
using System.Linq;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Configuration;

/// <summary>
/// Encapsulates duration, weighting, and dependency metadata used by the anomaly injector
/// so that anomaly generation can be tuned without touching the injection logic itself.
/// </summary>
public sealed record AnomalyConfiguration(
    IReadOnlyDictionary<AnomalyType, AnomalyDurationProfile> Durations,
    IReadOnlyDictionary<AnomalyType, double> Weights,
    IReadOnlyList<AnomalyDependency> Dependencies,
    double CombinationProbability)
{
    public TimeSpan GetDuration(AnomalyType type, Difficulty difficulty, Random rnd)
    {
        if (!Durations.TryGetValue(type, out var durationProfile))
        {
            return TimeSpan.FromMinutes(60);
        }

        return durationProfile.Sample(difficulty, rnd);
    }

    public AnomalyType? PickWeighted(Random rnd, IEnumerable<AnomalyType> allowed)
    {
        var filtered = allowed
            .Where(t => t != AnomalyType.None)
            .Where(t => Weights.TryGetValue(t, out var weight) && weight > 0)
            .Select(t => (Type: t, Weight: Weights[t]))
            .ToArray();

        if (filtered.Length == 0)
        {
            return null;
        }

        var totalWeight = filtered.Sum(t => t.Weight);
        var roll = rnd.NextDouble() * totalWeight;
        foreach (var (type, weight) in filtered)
        {
            if (roll < weight)
            {
                return type;
            }

            roll -= weight;
        }

        return filtered.Last().Type;
    }
}

/// <summary>
/// Specifies the duration span for each difficulty level and supplies sampling utilities.
/// </summary>
public sealed record AnomalyDurationProfile(DurationRange Easy, DurationRange Normal, DurationRange Hard)
{
    public TimeSpan Sample(Difficulty difficulty, Random rnd)
    {
        return difficulty switch
        {
            Difficulty.Easy => Easy.Sample(rnd),
            Difficulty.Normal => Normal.Sample(rnd),
            Difficulty.Hard => Hard.Sample(rnd),
            _ => Normal.Sample(rnd)
        };
    }
}

/// <summary>
/// Binds an anomaly type to one or more follow-up anomalies along with a delay window.
/// </summary>
public sealed record AnomalyDependency(AnomalyType Trigger, IReadOnlyList<AnomalyType> FollowUps, DurationRange DelayRange);

/// <summary>
/// Inclusive min/max pair to make duration sampling self-documenting.
/// </summary>
public sealed record DurationRange(TimeSpan Min, TimeSpan Max)
{
    public TimeSpan Sample(Random rnd)
    {
        if (Max <= Min)
        {
            return Min;
        }

        var spanMinutes = rnd.NextDouble() * (Max - Min).TotalMinutes;
        return Min + TimeSpan.FromMinutes(spanMinutes);
    }
}

/// <summary>
/// Factory for the built-in anomaly configuration used by the CLI entry point.
/// </summary>
public static class AnomalyConfigurationFactory
{
    public static AnomalyConfiguration CreateDefault()
    {
        var durations = new Dictionary<AnomalyType, AnomalyDurationProfile>
        {
            [AnomalyType.BatteryCutoff] = new(
                new DurationRange(TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(90)),
                new DurationRange(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(180)),
                new DurationRange(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(240))),
            [AnomalyType.BatteryDegradation] = new(
                new DurationRange(TimeSpan.FromMinutes(240), TimeSpan.FromMinutes(360)),
                new DurationRange(TimeSpan.FromMinutes(360), TimeSpan.FromMinutes(720)),
                new DurationRange(TimeSpan.FromMinutes(480), TimeSpan.FromMinutes(900))),
            [AnomalyType.NetControllerFailure] = new(
                new DurationRange(TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(120)),
                new DurationRange(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(240)),
                new DurationRange(TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(360))),
            [AnomalyType.NetDegradation] = new(
                new DurationRange(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(180)),
                new DurationRange(TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(360)),
                new DurationRange(TimeSpan.FromMinutes(180), TimeSpan.FromMinutes(600))),
            [AnomalyType.SpeakersLineOpen] = new(
                new DurationRange(TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(180)),
                new DurationRange(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(240)),
                new DurationRange(TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(360))),
            [AnomalyType.SpeakersLineShort] = new(
                new DurationRange(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(50)),
                new DurationRange(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(120)),
                new DurationRange(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(160))),
            [AnomalyType.SpeakersPartialFailure] = new(
                new DurationRange(TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(300)),
                new DurationRange(TimeSpan.FromMinutes(180), TimeSpan.FromMinutes(420)),
                new DurationRange(TimeSpan.FromMinutes(240), TimeSpan.FromMinutes(520))),
            [AnomalyType.CoolingDegradation] = new(
                new DurationRange(TimeSpan.FromMinutes(120), TimeSpan.FromMinutes(400)),
                new DurationRange(TimeSpan.FromMinutes(180), TimeSpan.FromMinutes(600)),
                new DurationRange(TimeSpan.FromMinutes(240), TimeSpan.FromMinutes(720))),
            [AnomalyType.TempSensorFailure] = new(
                new DurationRange(TimeSpan.FromMinutes(40), TimeSpan.FromMinutes(150)),
                new DurationRange(TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(180)),
                new DurationRange(TimeSpan.FromMinutes(90), TimeSpan.FromMinutes(220)))
        };

        var weights = new Dictionary<AnomalyType, double>
        {
            [AnomalyType.BatteryCutoff] = 0.8,
            [AnomalyType.BatteryDegradation] = 1.2,
            [AnomalyType.NetControllerFailure] = 1.0,
            [AnomalyType.NetDegradation] = 1.4,
            [AnomalyType.SpeakersLineOpen] = 0.9,
            [AnomalyType.SpeakersLineShort] = 1.3,
            [AnomalyType.SpeakersPartialFailure] = 1.1,
            [AnomalyType.CoolingDegradation] = 1.5,
            [AnomalyType.TempSensorFailure] = 0.6
        };

        var dependencies = new List<AnomalyDependency>
        {
            new(AnomalyType.CoolingDegradation,
                new[] { AnomalyType.TempSensorFailure, AnomalyType.NetDegradation },
                new DurationRange(TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(120))),
            new(AnomalyType.BatteryCutoff,
                new[] { AnomalyType.NetDegradation },
                new DurationRange(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(90))),
            new(AnomalyType.NetDegradation,
                new[] { AnomalyType.NetControllerFailure },
                new DurationRange(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(180))),
            new(AnomalyType.SpeakersPartialFailure,
                new[] { AnomalyType.SpeakersLineShort },
                new DurationRange(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(90)))
        };

        return new AnomalyConfiguration(durations, weights, dependencies, CombinationProbability: 0.35);
    }
}
