using System.Text;
using System.Text.Json;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Reporting;

public sealed class ReportBuilder
{
    public string BuildMarkdown(DataQualityReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Data Quality Report — {report.DatasetName}");
        sb.AppendLine();
        sb.AppendLine($"Generated at: {report.GeneratedAtUtc:O}");
        sb.AppendLine($"Records: {report.RecordCount}");
        sb.AppendLine($"Outcome: **{report.Summary.Outcome}** — {report.Summary.Details}");
        sb.AppendLine();

        foreach (var analyzer in report.AnalyzerResults)
        {
            sb.AppendLine($"## {analyzer.Name}");
            if (analyzer.Metrics.Any())
            {
                sb.AppendLine();
                sb.AppendLine("| Metric | Value |");
                sb.AppendLine("| --- | --- |");
                foreach (var metric in analyzer.Metrics)
                {
                    sb.AppendLine($"| {metric.Key} | {FormatMetric(metric.Value)} |");
                }
                sb.AppendLine();
            }

            if (analyzer.Issues.Any())
            {
                foreach (var issue in analyzer.Issues)
                {
                    sb.AppendLine($"- {issue.Severity}: {issue.Message}{(string.IsNullOrEmpty(issue.Context) ? string.Empty : $" ({issue.Context})")}");
                }
            }
            else
            {
                sb.AppendLine("- No issues detected");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    public string BuildJson(DataQualityReport report)
    {
        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string FormatMetric(object value) => value switch
    {
        null => "-",
        Array arr => string.Join(", ", arr.Cast<object>()),
        IEnumerable<double> doubles => string.Join(", ", doubles.Select(d => d.ToString("0.###"))),
        _ => value.ToString() ?? string.Empty
    };
}
