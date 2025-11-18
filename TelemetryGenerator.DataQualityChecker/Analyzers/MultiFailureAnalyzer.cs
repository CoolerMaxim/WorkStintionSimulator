using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class MultiFailureAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("MultiFailureAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var combinedFailures = records.Count(r => r.SignalStrength < -100 && r.CpuTemperature > 85 && r.BatteryVoltage < 22 && r.AmplifierOutPower < 5);
        if (combinedFailures == 0)
        {
            result.AddIssue(IssueSeverity.Warning, "No records combine low signal, high temp, low voltage and reduced amplifier power");
        }
        else
        {
            result.AddIssue(IssueSeverity.Info, $"Found {combinedFailures} multi-factor failure samples");
        }

        result.AddMetric("combinedFailureCount", combinedFailures);
        return result;
    }
}
