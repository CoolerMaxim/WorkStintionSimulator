using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class AnomalyAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("AnomalyAnalyzer");
        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        var anomalyCount = records.Count(r => r.IsAnomaly);
        var anomalyRate = anomalyCount / (double)records.Count;
        result.AddMetric("anomalyRate", Math.Round(anomalyRate, 4));

        var typeDistribution = Enum.GetValues<AnomalyType>()
            .ToDictionary(type => type.ToString(), type => records.Count(r => r.AnomalyType == type));
        result.AddMetric("anomalyDistribution", typeDistribution);

        var missingTypes = typeDistribution.Where(kv => kv.Value == 0 && kv.Key != nameof(AnomalyType.None)).Select(kv => kv.Key).ToList();
        if (missingTypes.Any())
        {
            result.AddIssue(IssueSeverity.Warning, "Some anomaly types are missing", string.Join(", ", missingTypes));
        }

        var isolatedAnomalies = CountIsolatedAnomalies(records);
        if (isolatedAnomalies > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"Found {isolatedAnomalies} isolated anomalies without neighboring context");
        }

        ValidateLabelConsistency(records, result);

        return result;
    }

    private static int CountIsolatedAnomalies(IReadOnlyList<TelemetryRecord> records)
    {
        var grouped = records.GroupBy(r => r.WorkStationId);
        var isolated = 0;

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(r => r.Timestamp).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (!ordered[i].IsAnomaly)
                {
                    continue;
                }

                var prev = i > 0 ? ordered[i - 1] : null;
                var next = i < ordered.Count - 1 ? ordered[i + 1] : null;
                var hasNeighbor = (prev?.IsAnomaly ?? false) || (next?.IsAnomaly ?? false);
                if (!hasNeighbor)
                {
                    isolated++;
                }
            }
        }

        return isolated;
    }

    private static void ValidateLabelConsistency(IReadOnlyList<TelemetryRecord> records, AnalyzerResult result)
    {
        var batteryCutoffMismatch = records.Count(r => r.AnomalyType == AnomalyType.BatteryCutoff && r.BatteryVoltage > 22);
        if (batteryCutoffMismatch > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"BatteryCutoff anomalies without low voltage: {batteryCutoffMismatch}");
        }

        var netFailureMismatches = records.Count(r => r.AnomalyType == AnomalyType.NetControllerFailure && r.NetworkLatency < 500 && r.NetworkLatency != 9999);
        if (netFailureMismatches > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"NetFailure anomalies without high latency: {netFailureMismatches}");
        }

        var speakerPartialMismatches = records.Count(r => r.AnomalyType == AnomalyType.SpeakersPartialFailure && r.AmplifierOutPower > 10);
        if (speakerPartialMismatches > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"SpeakersPartialFailure anomalies without lowered amplifier output: {speakerPartialMismatches}");
        }
    }
}
