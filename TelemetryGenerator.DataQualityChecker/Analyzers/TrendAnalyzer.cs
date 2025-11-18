using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class TrendAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("TrendAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var batteryTrendOk = CheckTrend(records, AnomalyType.BatteryCutoff, r => r.BatteryVoltage, expectedDirection: -1);
        var latencyTrendOk = CheckTrend(records, AnomalyType.NetControllerFailure, r => r.NetworkLatency, expectedDirection: 1);
        var tempTrendOk = CheckTrend(records, AnomalyType.CoolingDegradation, r => r.CpuTemperature, expectedDirection: 1);

        if (!batteryTrendOk)
        {
            result.AddIssue(IssueSeverity.Warning, "BatteryCutoff anomalies do not have a preceding downward voltage trend");
        }

        if (!latencyTrendOk)
        {
            result.AddIssue(IssueSeverity.Warning, "NetFailure anomalies lack growing latency trend");
        }

        if (!tempTrendOk)
        {
            result.AddIssue(IssueSeverity.Warning, "Cooling degradation anomalies do not show rising temperature before failure");
        }

        return result;
    }

    private static bool CheckTrend(IReadOnlyList<TelemetryRecord> records, AnomalyType type, Func<TelemetryRecord, double> selector, int expectedDirection)
    {
        var grouped = records.GroupBy(r => r.WorkStationId);
        var ok = true;

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(r => r.Timestamp).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].AnomalyType != type)
                {
                    continue;
                }

                var window = ordered.Skip(Math.Max(0, i - 3)).Take(3).Select(selector).ToList();
                if (window.Count < 3)
                {
                    continue;
                }

                var trend = window.Last() - window.First();
                if ((expectedDirection < 0 && trend >= 0) || (expectedDirection > 0 && trend <= 0))
                {
                    ok = false;
                }
            }
        }

        return ok;
    }
}
