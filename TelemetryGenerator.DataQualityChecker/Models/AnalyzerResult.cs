using System.Text.Json.Serialization;

namespace TelemetryGenerator.DataQualityChecker.Models;

public sealed class AnalyzerResult
{
    public string Name { get; }

    public List<AnalysisIssue> Issues { get; } = new();

    public Dictionary<string, object> Metrics { get; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool HasCriticalIssues => Issues.Any(issue => issue.Severity == IssueSeverity.Critical);

    [JsonIgnore]
    public bool HasWarnings => Issues.Any(issue => issue.Severity == IssueSeverity.Warning);

    public AnalyzerResult(string name)
    {
        Name = name;
    }

    public AnalyzerResult AddIssue(IssueSeverity severity, string message, string? context = null)
    {
        Issues.Add(new AnalysisIssue(severity, message, context));
        return this;
    }

    public AnalyzerResult AddMetric(string name, object value)
    {
        Metrics[name] = value;
        return this;
    }
}
