using System.Text.Json.Serialization;

namespace TelemetryGenerator.DataQualityChecker.Models;

public sealed class DataQualityReport
{
    public string DatasetName { get; init; } = string.Empty;

    public int RecordCount { get; init; }

    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;

    public QualitySummary Summary { get; init; } = new(QualityOutcome.Fail, "Report not generated");

    public IReadOnlyList<AnalyzerResult> AnalyzerResults { get; init; } = Array.Empty<AnalyzerResult>();

    [JsonIgnore]
    public bool HasCriticalIssues => AnalyzerResults.Any(r => r.HasCriticalIssues);
}
