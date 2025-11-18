namespace TelemetryGenerator.DataQualityChecker.Models;

public sealed record CsvLoadResult(
    IReadOnlyList<TelemetryRecord> Records,
    IReadOnlyList<AnalysisIssue> Issues,
    IReadOnlyList<string> MissingColumns);
