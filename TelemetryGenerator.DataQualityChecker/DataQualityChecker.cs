using Microsoft.Extensions.Options;
using TelemetryGenerator.DataQualityChecker.Analyzers;
using TelemetryGenerator.DataQualityChecker.Configuration;
using TelemetryGenerator.DataQualityChecker.Models;
using TelemetryGenerator.DataQualityChecker.Reporting;
using TelemetryGenerator.DataQualityChecker.Services;

namespace TelemetryGenerator.DataQualityChecker;

public sealed class DataQualityChecker
{
    private readonly DataQualityCheckerOptions _options;
    private readonly CsvLoader _loader;
    private readonly StructureAnalyzer? _structureAnalyzer;
    private readonly TimeGridAnalyzer? _timeGridAnalyzer;
    private readonly PhysicsAnalyzer? _physicsAnalyzer;
    private readonly AnomalyAnalyzer? _anomalyAnalyzer;
    private readonly ScenarioAnalyzer? _scenarioAnalyzer;
    private readonly MLFitnessAnalyzer? _mlFitnessAnalyzer;
    private readonly ReportBuilder _reportBuilder;

    public DataQualityChecker(
        IOptionsSnapshot<DataQualityCheckerOptions> options,
        CsvLoader loader,
        StructureAnalyzer structureAnalyzer,
        TimeGridAnalyzer timeGridAnalyzer,
        PhysicsAnalyzer physicsAnalyzer,
        AnomalyAnalyzer anomalyAnalyzer,
        ScenarioAnalyzer scenarioAnalyzer,
        MLFitnessAnalyzer mlFitnessAnalyzer,
        ReportBuilder reportBuilder)
    {
        _options = options.Value;
        _loader = loader;
        _structureAnalyzer = _options.EnableStructureAnalyzer ? structureAnalyzer : null;
        _timeGridAnalyzer = _options.EnableTimeGridAnalyzer ? timeGridAnalyzer : null;
        _physicsAnalyzer = _options.EnablePhysicsAnalyzer ? physicsAnalyzer : null;
        _anomalyAnalyzer = _options.EnableAnomalyAnalyzer ? anomalyAnalyzer : null;
        _scenarioAnalyzer = _options.EnableScenarioAnalyzer ? scenarioAnalyzer : null;
        _mlFitnessAnalyzer = _options.EnableMlFitnessAnalyzer ? mlFitnessAnalyzer : null;
        _reportBuilder = reportBuilder;
    }

    public (string markdown, string json, DataQualityReport report) Run(string csvPath, string datasetName)
    {
        var loadResult = _loader.Load(csvPath);
        var recordList = loadResult.Records;

        var results = new List<AnalyzerResult>();

        AddIfConfigured(_structureAnalyzer, analyzer => analyzer.Analyze(loadResult), results);

        if (recordList.Any())
        {
            AddIfConfigured(_timeGridAnalyzer, analyzer => analyzer.Analyze(recordList), results);
            AddIfConfigured(_physicsAnalyzer, analyzer => analyzer.Analyze(recordList), results);
            AddIfConfigured(_anomalyAnalyzer, analyzer => analyzer.Analyze(recordList), results);
            AddIfConfigured(_scenarioAnalyzer, analyzer => analyzer.Analyze(recordList), results);
            if (_mlFitnessAnalyzer is not null)
            {
                results.AddRange(_mlFitnessAnalyzer.Analyze(recordList));
            }
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

    private static void AddIfConfigured<TAnalyzer>(TAnalyzer? analyzer, Func<TAnalyzer, AnalyzerResult> analyze, List<AnalyzerResult> results)
        where TAnalyzer : class
    {
        if (analyzer is null)
        {
            return;
        }

        results.Add(analyze(analyzer));
    }
}
