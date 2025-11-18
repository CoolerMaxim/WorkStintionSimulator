using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class ClassBalanceAnalyzer
{
    private readonly int _minCountPerClass;

    public ClassBalanceAnalyzer(int minCountPerClass = 10)
    {
        _minCountPerClass = minCountPerClass;
    }

    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("ClassBalanceAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var healthCounts = Enum.GetValues<HealthState>().ToDictionary(state => state, state => records.Count(r => r.HealthState == state));
        result.AddMetric("healthStateBalance", healthCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

        foreach (var kv in healthCounts)
        {
            if (kv.Value < _minCountPerClass)
            {
                result.AddIssue(IssueSeverity.Warning, $"HealthState '{kv.Key}' has only {kv.Value} samples (min {_minCountPerClass})");
            }
        }

        var anomalyCounts = Enum.GetValues<AnomalyType>().ToDictionary(type => type, type => records.Count(r => r.AnomalyType == type));
        result.AddMetric("anomalyBalance", anomalyCounts.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

        foreach (var kv in anomalyCounts.Where(kv => kv.Key != AnomalyType.None))
        {
            if (kv.Value < _minCountPerClass)
            {
                result.AddIssue(IssueSeverity.Warning, $"AnomalyType '{kv.Key}' has only {kv.Value} samples (min {_minCountPerClass})");
            }
        }

        return result;
    }
}
