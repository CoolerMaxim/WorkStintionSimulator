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

    public QualityCheckStep(IOptions<PipelineOptions> options, ILogger<QualityCheckStep> logger)
    {
        _logger = logger;
        _options = options.Value;
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
        var checker = new DataQualityChecker();
        var (markdown, json, report) = checker.Run(_options.OutputPath, _options.DatasetName!);

        File.WriteAllText(_options.QualityMarkdownPath, markdown);
        File.WriteAllText(_options.QualityJsonPath, json);

        var exitCode = report.Summary.Outcome == QualityOutcome.Fail ? 2 : 0;
        _logger.LogInformation("Data quality outcome: {Outcome} ({Details})", report.Summary.Outcome, report.Summary.Details);
        return Task.FromResult(exitCode);
    }
}
