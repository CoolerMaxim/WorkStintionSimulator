using TelemetryGenerator.DataQualityChecker.Analyzers;
using TelemetryGenerator.DataQualityChecker.Models;
using TelemetryGenerator.DataQualityChecker.Reporting;
using TelemetryGenerator.DataQualityChecker.Services;

namespace TelemetryGenerator.DataQualityChecker;

public sealed class DataQualityChecker
{
    private readonly CsvLoader _loader;
    private readonly StructureAnalyzer _structureAnalyzer;
    private readonly TimeGridAnalyzer _timeGridAnalyzer;
    private readonly PhysicsAnalyzer _physicsAnalyzer;
    private readonly AnomalyAnalyzer _anomalyAnalyzer;
    private readonly ScenarioAnalyzer _scenarioAnalyzer;
    private readonly MLFitnessAnalyzer _mlFitnessAnalyzer;
    private readonly ReportBuilder _reportBuilder;

    public DataQualityChecker()
    {
        _loader = new CsvLoader();
        _structureAnalyzer = new StructureAnalyzer();
        _timeGridAnalyzer = new TimeGridAnalyzer();
        _physicsAnalyzer = new PhysicsAnalyzer();
        _anomalyAnalyzer = new AnomalyAnalyzer();
        _scenarioAnalyzer = new ScenarioAnalyzer();
        _mlFitnessAnalyzer = new MLFitnessAnalyzer();
        _reportBuilder = new ReportBuilder();
    }

    public (string markdown, string json, DataQualityReport report) Run(string csvPath, string datasetName)
    {
        var loadResult = _loader.Load(csvPath);
        var structure = _structureAnalyzer.Analyze(loadResult);
        var recordList = loadResult.Records;

        var results = new List<AnalyzerResult> { structure };

        if (recordList.Any())
        {
            results.Add(_timeGridAnalyzer.Analyze(recordList));
            results.Add(_physicsAnalyzer.Analyze(recordList));
            results.Add(_anomalyAnalyzer.Analyze(recordList));
            results.Add(_scenarioAnalyzer.Analyze(recordList));
            results.AddRange(_mlFitnessAnalyzer.Analyze(recordList));
        }

        var summary = BuildSummary(results);
        var report = new DataQualityReport
        {
            DatasetName = datasetName,
            RecordCount = recordList.Count,
            AnalyzerResults = results,
            Summary = summary
        };

        var markdown = _reportBuilder.BuildMarkdown(report);
        var json = _reportBuilder.BuildJson(report);
        return (markdown, json, report);
    }

    private static QualitySummary BuildSummary(IEnumerable<AnalyzerResult> results)
    {
        var issues = results.SelectMany(r => r.Issues).ToList();
        if (issues.Any(i => i.Severity == IssueSeverity.Critical))
        {
            return new QualitySummary(QualityOutcome.Fail, "Critical data quality issues detected");
        }

        if (issues.Any(i => i.Severity == IssueSeverity.Warning))
        {
            return new QualitySummary(QualityOutcome.PassWithWarnings, "Warnings found — review recommended before training");
        }

        return new QualitySummary(QualityOutcome.Pass, "Dataset is ready for model training");
    }
}
