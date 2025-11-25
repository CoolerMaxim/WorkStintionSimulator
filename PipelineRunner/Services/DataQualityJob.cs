using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Configuration;
using PipelineRunner.Options;
using TelemetryGenerator.DataQualityChecker;
using TelemetryGenerator.DataQualityChecker.Models;

namespace PipelineRunner.Services;

internal sealed class DataQualityJob : IDataQualityJob
{
    private readonly ILogger<DataQualityJob> _logger;
    private readonly DatasetConfig _datasetConfig;
    private readonly ValidationSettings _validationSettings;
    private readonly DataQualityChecker _dataQualityChecker;

    public DataQualityJob(
        PipelineConfig pipelineConfig,
        IOptions<ValidationSettings> validationOptions,
        DataQualityChecker dataQualityChecker,
        ILogger<DataQualityJob> logger)
    {
        _logger = logger;
        _datasetConfig = pipelineConfig.Dataset;
        _validationSettings = validationOptions.Value;
        _dataQualityChecker = dataQualityChecker;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Running data quality checks with csv={Csv}, dataset={Dataset}, markdown={Markdown}, json={Json}",
            _datasetConfig.OutputPath,
            _datasetConfig.DatasetName,
            _datasetConfig.QualityMarkdownPath,
            _datasetConfig.QualityJsonPath);

        _logger.LogDebug(
            "Analyzer configuration: structure={Structure}, timeGrid={TimeGrid}, physics={Physics}, anomaly={Anomaly}, scenario={Scenario}, mlFitness={MlFitness}",
            _validationSettings.EnableStructureAnalyzer,
            _validationSettings.EnableTimeGridAnalyzer,
            _validationSettings.EnablePhysicsAnalyzer,
            _validationSettings.EnableAnomalyAnalyzer,
            _validationSettings.EnableScenarioAnalyzer,
            _validationSettings.EnableMlFitnessAnalyzer);

        var qualityDirectory = Path.GetDirectoryName(_datasetConfig.QualityMarkdownPath)
            ?? _datasetConfig.WorkingDirectory;
        Directory.CreateDirectory(qualityDirectory);

        var (markdown, json, report) = _dataQualityChecker.Run(
            _datasetConfig.OutputPath,
            _datasetConfig.DatasetName);

        File.WriteAllText(_datasetConfig.QualityMarkdownPath, markdown);
        File.WriteAllText(_datasetConfig.QualityJsonPath, json);

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
