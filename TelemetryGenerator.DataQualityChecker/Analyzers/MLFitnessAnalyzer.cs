using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class MLFitnessAnalyzer
{
    private readonly ClassBalanceAnalyzer _classBalanceAnalyzer;
    private readonly SeparabilityAnalyzer _separabilityAnalyzer;
    private readonly DataLeakageAnalyzer _dataLeakageAnalyzer;
    private readonly TrendAnalyzer _trendAnalyzer;
    private readonly MultiFailureAnalyzer _multiFailureAnalyzer;
    private readonly ScenarioCoverageAnalyzer _scenarioCoverageAnalyzer;

    public MLFitnessAnalyzer(ClassBalanceAnalyzer? classBalanceAnalyzer = null)
    {
        _classBalanceAnalyzer = classBalanceAnalyzer ?? new ClassBalanceAnalyzer();
        _separabilityAnalyzer = new SeparabilityAnalyzer();
        _dataLeakageAnalyzer = new DataLeakageAnalyzer();
        _trendAnalyzer = new TrendAnalyzer();
        _multiFailureAnalyzer = new MultiFailureAnalyzer();
        _scenarioCoverageAnalyzer = new ScenarioCoverageAnalyzer();
    }

    public IReadOnlyList<AnalyzerResult> Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        return new List<AnalyzerResult>
        {
            _classBalanceAnalyzer.Analyze(records),
            _separabilityAnalyzer.Analyze(records),
            _dataLeakageAnalyzer.Analyze(records),
            _trendAnalyzer.Analyze(records),
            _multiFailureAnalyzer.Analyze(records),
            _scenarioCoverageAnalyzer.Analyze(records)
        };
    }
}
