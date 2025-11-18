using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class ScenarioAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("ScenarioAnalyzer");

        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var scenarioCounts = records.GroupBy(r => r.ScenarioId).ToDictionary(g => g.Key, g => g.Count());
        if (scenarioCounts.TryGetValue(string.Empty, out var emptyCount) && emptyCount > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"{emptyCount} records are missing ScenarioId");
        }

        result.AddMetric("scenarioCounts", scenarioCounts);

        var difficultyCounts = records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Difficulty) ? "Unknown" : r.Difficulty)
            .ToDictionary(g => g.Key, g => g.Count());
        result.AddMetric("difficultyCounts", difficultyCounts);

        foreach (var difficulty in Enum.GetNames<Difficulty>())
        {
            if (!difficultyCounts.ContainsKey(difficulty))
            {
                result.AddIssue(IssueSeverity.Warning, $"Difficulty '{difficulty}' is missing from dataset");
            }
        }

        var healthCounts = Enum.GetValues<HealthState>()
            .ToDictionary(state => state.ToString(), state => records.Count(r => r.HealthState == state));
        result.AddMetric("healthCounts", healthCounts);

        var missingHealthStates = healthCounts.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (missingHealthStates.Any())
        {
            result.AddIssue(IssueSeverity.Warning, "HealthState coverage is incomplete", string.Join(", ", missingHealthStates));
        }

        return result;
    }
}
