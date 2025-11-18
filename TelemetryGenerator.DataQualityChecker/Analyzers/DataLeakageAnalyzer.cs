using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class DataLeakageAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("DataLeakageAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        CheckLatencyLeakage(records, result);
        CheckVoltageLeakage(records, result);
        CheckPerfectProxies(records, result);

        return result;
    }

    private static void CheckLatencyLeakage(IReadOnlyList<TelemetryRecord> records, AnalyzerResult result)
    {
        var latencyAtFailure = records.Where(r => r.AnomalyType == AnomalyType.NetControllerFailure).Select(r => r.NetworkLatency).ToList();
        if (latencyAtFailure.Any())
        {
            var unique = latencyAtFailure.Distinct().ToList();
            if (unique.Count == 1 && unique[0] == 9999)
            {
                result.AddIssue(IssueSeverity.Warning, "NetworkLatency always 9999 for NetFailure — add negative examples to avoid leakage");
            }
        }
    }

    private static void CheckVoltageLeakage(IReadOnlyList<TelemetryRecord> records, AnalyzerResult result)
    {
        var cutoffVoltages = records.Where(r => r.AnomalyType == AnomalyType.BatteryCutoff).Select(r => r.BatteryVoltage).ToList();
        if (cutoffVoltages.Any())
        {
            var maxVoltage = cutoffVoltages.Max();
            if (maxVoltage == cutoffVoltages.Min())
            {
                result.AddIssue(IssueSeverity.Warning, "BatteryCutoff always uses the same voltage value — looks like direct label encoding");
            }
        }
    }

    private static void CheckPerfectProxies(IReadOnlyList<TelemetryRecord> records, AnalyzerResult result)
    {
        var map = records.GroupBy(r => r.RestartCount).Select(g => new { Restart = g.Key, HealthStates = g.Select(r => r.HealthState).Distinct().Count() }).ToList();
        if (map.Any(item => item.HealthStates == 1))
        {
            result.AddIssue(IssueSeverity.Info, "Found RestartCount values that map 1:1 to health labels — verify features are not derived");
        }
    }
}
