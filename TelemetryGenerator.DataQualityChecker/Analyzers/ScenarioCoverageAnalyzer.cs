using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class ScenarioCoverageAnalyzer
{
    private const int MinimumSpanMinutes = 5;

    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("ScenarioCoverageAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var scenarioWindows = new Dictionary<string, TimeSpan>();
        foreach (var scenario in records.Where(r => !string.IsNullOrWhiteSpace(r.ScenarioId)).GroupBy(r => r.ScenarioId))
        {
            var ordered = scenario.OrderBy(r => r.Timestamp).ToList();
            var span = ordered.Last().Timestamp - ordered.First().Timestamp;
            scenarioWindows[scenario.Key] = span;
            if (span < TimeSpan.FromMinutes(MinimumSpanMinutes))
            {
                result.AddIssue(IssueSeverity.Warning, $"Scenario '{scenario.Key}' is too short ({span.TotalMinutes:F1} min)");
            }
        }

        result.AddMetric("scenarioWindowsMinutes", scenarioWindows.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value.TotalMinutes, 2)));

        var hasPrePostWindows = records.GroupBy(r => r.ScenarioId)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .All(g => g.Any(r => r.Timestamp == g.Min(x => x.Timestamp)) && g.Any(r => r.Timestamp == g.Max(x => x.Timestamp)));
        if (!hasPrePostWindows)
        {
            result.AddIssue(IssueSeverity.Warning, "Some scenarios missing warm-up or cool-down windows");
        }

        return result;
    }
}
