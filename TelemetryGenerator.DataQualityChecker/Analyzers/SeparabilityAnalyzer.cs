using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class SeparabilityAnalyzer
{
    private static readonly string[] NumericSignals =
    {
        nameof(TelemetryRecord.BatteryVoltage),
        nameof(TelemetryRecord.NetworkLatency),
        nameof(TelemetryRecord.CpuTemperature),
        nameof(TelemetryRecord.AmplifierOutPower)
    };

    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("SeparabilityAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var warnings = 0;
        foreach (var signal in NumericSignals)
        {
            var spread = ComputeSpread(records, signal);
            result.AddMetric($"spread_{signal}", spread);
            if (spread < 0.15)
            {
                warnings++;
                result.AddIssue(IssueSeverity.Warning, $"Weak separation between health states on {signal}");
            }
        }

        if (warnings == 0)
        {
            result.AddIssue(IssueSeverity.Info, "Health states show good separability across tracked signals");
        }

        return result;
    }

    private static double ComputeSpread(IReadOnlyList<TelemetryRecord> records, string property)
    {
        var perState = Enum.GetValues<HealthState>()
            .ToDictionary(state => state, state => GetSignalValues(records, property, state));

        var means = perState.Values.Where(list => list.Any()).Select(list => list.Average()).ToList();
        if (means.Count < 2)
        {
            return 0d;
        }

        var max = means.Max();
        var min = means.Min();
        var overall = records.Select(r => GetValue(r, property)).Where(v => !double.IsNaN(v)).ToList();
        if (!overall.Any())
        {
            return 0d;
        }

        var overallStd = StandardDeviation(overall);
        return overallStd == 0 ? 0d : (max - min) / overallStd;
    }

    private static List<double> GetSignalValues(IEnumerable<TelemetryRecord> records, string property, HealthState state)
    {
        return records
            .Where(r => r.HealthState == state)
            .Select(r => GetValue(r, property))
            .Where(v => !double.IsNaN(v))
            .ToList();
    }

    private static double GetValue(TelemetryRecord record, string property) => property switch
    {
        nameof(TelemetryRecord.BatteryVoltage) => record.BatteryVoltage,
        nameof(TelemetryRecord.NetworkLatency) => record.NetworkLatency,
        nameof(TelemetryRecord.CpuTemperature) => record.CpuTemperature,
        nameof(TelemetryRecord.AmplifierOutPower) => record.AmplifierOutPower,
        _ => double.NaN
    };

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }
}
