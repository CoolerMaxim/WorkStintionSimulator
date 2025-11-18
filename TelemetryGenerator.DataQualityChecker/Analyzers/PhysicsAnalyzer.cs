using TelemetryGenerator.DataQualityChecker.Models;

namespace TelemetryGenerator.DataQualityChecker.Analyzers;

public sealed class PhysicsAnalyzer
{
    public AnalyzerResult Analyze(IReadOnlyList<TelemetryRecord> records)
    {
        var result = new AnalyzerResult("PhysicsAnalyzer");

        if (!records.Any())
        {
            result.AddIssue(IssueSeverity.Critical, "Dataset is empty");
            return result;
        }

        int rangeViolations = 0;
        int invariantViolations = 0;

        foreach (var record in records)
        {
            rangeViolations += CheckRanges(result, record);
            invariantViolations += CheckInvariants(result, record);
        }

        result.AddMetric("rangeViolations", rangeViolations);
        result.AddMetric("invariantViolations", invariantViolations);

        var activeCpu = records.Where(r => r.SoundStatus).Select(r => r.CpuTemperature).ToList();
        var idleCpu = records.Where(r => !r.SoundStatus).Select(r => r.CpuTemperature).ToList();
        if (activeCpu.Any() && idleCpu.Any())
        {
            var activeMean = activeCpu.Average();
            var idleMean = idleCpu.Average();
            if (activeMean <= idleMean)
            {
                result.AddIssue(IssueSeverity.Warning, "CPU temperature does not increase during active sound transmission");
            }
            result.AddMetric("cpuTempActiveMean", Math.Round(activeMean, 2));
            result.AddMetric("cpuTempIdleMean", Math.Round(idleMean, 2));
        }

        return result;
    }

    private static int CheckRanges(AnalyzerResult result, TelemetryRecord record)
    {
        var violations = 0;

        bool OutOfRange(double value, double min, double max) => double.IsNaN(value) || value < min || value > max;

        if (OutOfRange(record.BatteryVoltage, 21, 27.5))
        {
            result.AddIssue(IssueSeverity.Warning, $"BatteryVoltage out of range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (OutOfRange(record.CpuTemperature, 0, 100))
        {
            result.AddIssue(IssueSeverity.Warning, $"CpuTemperature out of range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (OutOfRange(record.InsideTemperature, -10, 80))
        {
            result.AddIssue(IssueSeverity.Warning, $"InsideTemperature out of range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (record.DiskSpaceUse is < 0 or > 100)
        {
            result.AddIssue(IssueSeverity.Warning, $"DiskSpaceUse out of range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (OutOfRange(record.SignalStrength, -110, -40))
        {
            result.AddIssue(IssueSeverity.Warning, $"SignalStrength out of range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (!(record.NetworkLatency is >= 0 and <= 2000) && record.NetworkLatency != 9999)
        {
            result.AddIssue(IssueSeverity.Warning, $"NetworkLatency out of expected range for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (record.SpeakersConfigured <= 0 && record.AmplifierOutPower > 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"AmplifierOutPower present with no speakers configured for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        return violations;
    }

    private static int CheckInvariants(AnalyzerResult result, TelemetryRecord record)
    {
        var violations = 0;

        if (record.SoundStatus && record.AmplifierOutPower <= 0)
        {
            result.AddIssue(IssueSeverity.Critical, $"SoundStatus is active but AmplifierOutPower is zero for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (!record.PowerStatus && !record.BatteryStatus && record.RestartCount == 0)
        {
            result.AddIssue(IssueSeverity.Warning, $"No power and no battery but telemetry continues for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (record.NetworkLatency == 9999 && record.SignalStrength > -70)
        {
            result.AddIssue(IssueSeverity.Warning, $"High latency without weak signal for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        if (record.SpeakersEffective.HasValue && record.SpeakersEffective < record.SpeakersConfigured && record.AmplifierOutPower > 0 && record.AmplifierOutPower >= 10)
        {
            result.AddIssue(IssueSeverity.Warning, $"Amplifier power not reduced despite missing speakers for {record.WorkStationId} at {record.Timestamp:O}");
            violations++;
        }

        return violations;
    }
}
