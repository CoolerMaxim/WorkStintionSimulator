namespace TelemetryGenerator.DataQualityChecker.Models;

public sealed record AnalysisIssue(IssueSeverity Severity, string Message, string? Context = null)
{
    public override string ToString() => string.IsNullOrEmpty(Context) ? Message : $"{Message} ({Context})";
}
