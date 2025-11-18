using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class StructureAnalyzer
{
    public AnalyzerResult Analyze(CsvLoadResult loadResult)
    {
        var result = new AnalyzerResult("StructureAnalyzer");

        if (loadResult.MissingColumns.Any())
        {
            foreach (var missing in loadResult.MissingColumns)
            {
                result.AddIssue(IssueSeverity.Critical, $"Missing required column '{missing}'");
            }
        }

        if (!loadResult.Records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var duplicateCount = loadResult.Records
            .GroupBy(r => new { r.Timestamp, r.WorkStationId })
            .Sum(g => Math.Max(0, g.Count() - 1));

        if (duplicateCount > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"Found {duplicateCount} duplicate rows (Timestamp + WorkStationId)");
        }

        var nanSeries = loadResult.Records.Count(r => double.IsNaN(r.BatteryVoltage) || double.IsNaN(r.CpuTemperature));
        if (nanSeries > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"Detected {nanSeries} rows with NaN or invalid numeric values");
        }

        var missingIds = loadResult.Records.Count(r => string.IsNullOrWhiteSpace(r.WorkStationId));
        if (missingIds > 0)
        {
            result.AddIssue(IssueSeverity.Critical, "Some records have empty WorkStationId");
        }

        result.AddMetric("recordCount", loadResult.Records.Count);
        result.AddMetric("duplicateCount", duplicateCount);
        result.AddMetric("nanOrInvalidCount", nanSeries);

        return result;
    }
}
