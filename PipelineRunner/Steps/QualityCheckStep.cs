using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using TelemetryGenerator.DataQualityChecker;
using TelemetryGenerator.DataQualityChecker.Models;

namespace PipelineRunner.Steps;

internal sealed class QualityCheckStep
{
    private readonly ILogger<QualityCheckStep> _logger;
    private readonly PipelineOptions _options;
    private readonly DataQualityChecker _dataQualityChecker;

    public QualityCheckStep(
        IOptions<PipelineOptions> options,
        DataQualityChecker dataQualityChecker,
        ILogger<QualityCheckStep> logger)
    {
        _logger = logger;
        _options = options.Value;
        _dataQualityChecker = dataQualityChecker;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running data quality checks with csv={Csv}, dataset={Dataset}, markdown={Markdown}, json={Json}",
            _options.OutputPath,
            _options.DatasetName,
            _options.QualityMarkdownPath,
            _options.QualityJsonPath);

        Directory.CreateDirectory(Path.GetDirectoryName(_options.QualityMarkdownPath) ?? _options.WorkingDirectory);
        var (markdown, json, report) = _dataQualityChecker.Run(_options.OutputPath, _options.DatasetName!);

        File.WriteAllText(_options.QualityMarkdownPath, markdown);
        File.WriteAllText(_options.QualityJsonPath, json);

        var exitCode = report.Summary.Outcome == QualityOutcome.Fail ? 2 : 0;

        var allIssues = report.AnalyzerResults.SelectMany(result => result.Issues).ToList();
        var criticalCount = allIssues.Count(issue => issue.Severity == IssueSeverity.Critical);
        var warningCount = allIssues.Count(issue => issue.Severity == IssueSeverity.Warning);

        _logger.LogInformation(
            "Data quality outcome: {Outcome} ({Details}); records analyzed: {RecordCount}; analyzers: {AnalyzerCount}; issues: total={TotalIssues}, critical={CriticalCount}, warnings={WarningCount}",
            report.Summary.Outcome,
            report.Summary.Details,
            report.RecordCount,
            report.AnalyzerResults.Count,
            allIssues.Count,
            criticalCount,
            warningCount);

        if (allIssues.Count > 0)
        {
            _logger.LogInformation("Detailed data quality issues:");
            foreach (var analyzerResult in report.AnalyzerResults.Where(r => r.Issues.Count > 0))
            {
                var analyzerCritical = analyzerResult.Issues.Count(issue => issue.Severity == IssueSeverity.Critical);
                var analyzerWarnings = analyzerResult.Issues.Count(issue => issue.Severity == IssueSeverity.Warning);

                _logger.LogInformation(
                    "- {Analyzer}: {IssueCount} issues (critical: {CriticalCount}, warnings: {WarningCount})",
                    analyzerResult.Name,
                    analyzerResult.Issues.Count,
                    analyzerCritical,
                    analyzerWarnings);

                foreach (var issue in analyzerResult.Issues.OrderByDescending(i => i.Severity))
                {
                    var contextDetails = string.IsNullOrWhiteSpace(issue.Context)
                        ? string.Empty
                        : $" (context: {issue.Context})";

                    var logLevel = issue.Severity == IssueSeverity.Critical ? LogLevel.Error : LogLevel.Warning;
                    _logger.Log(
                        logLevel,
                        "   • {Severity}: {Message}{Context}",
                        issue.Severity,
                        issue.Message,
                        contextDetails);
                }
            }
        }

        return Task.FromResult(exitCode);
    }
}
