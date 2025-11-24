using System.Globalization;
using AI.Training.Features;
using AI.Training.Models;

namespace AI.Training.Data;

public class TrainingDataValidator
{
    public void Validate(IEnumerable<TelemetryRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        var recordList = records.ToList();
        var issues = new List<string>();

        if (recordList.Count == 0)
        {
            issues.Add("No records were loaded from the training dataset.");
        }

        var invalidTimestamps = recordList
            .Select(r => r.Timestamp)
            .Where(timestamp => !DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _))
            .Distinct()
            .ToList();

        if (invalidTimestamps.Count > 0)
        {
            issues.Add($"Unparseable timestamps detected: {string.Join(", ", invalidTimestamps)}.");
        }

        var invalidNumericRows = recordList
            .Select((record, index) => (record, index))
            .Where(entry => HasInvalidNumeric(entry.record))
            .Select(entry => entry.index)
            .ToList();

        if (invalidNumericRows.Count > 0)
        {
            issues.Add($"Found {invalidNumericRows.Count} records with NaN or infinite numeric fields (examples at rows: {string.Join(", ", invalidNumericRows.Take(5))}).");
        }

        var labelCounts = recordList
            .Select(FeatureExtractor.ResolveLabel)
            .GroupBy(label => label)
            .ToDictionary(g => g.Key, g => g.Count());

        if (labelCounts.Count < 2)
        {
            var distribution = labelCounts.Count == 0
                ? "(none)"
                : string.Join(", ", labelCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            issues.Add($"Training data must contain at least two classes, but found {labelCounts.Count} ({distribution}).");
        }

        if (issues.Count > 0)
        {
            throw new InvalidOperationException("Training data failed validation: " + string.Join(" ", issues));
        }
    }

    private static bool HasInvalidNumeric(TelemetryRecord record)
    {
        return IsInvalidNumber(record.BatteryVoltage)
               || IsInvalidNumber(record.CpuTemperature)
               || IsInvalidNumber(record.InsideTemperature)
               || IsInvalidNumber(record.AmplifierOutPower)
               || IsInvalidNumber(record.NetworkLatencyMs)
               || IsInvalidNumber(record.SpeakersConfigured)
               || IsInvalidNumber(record.NodeUptimeMinutes)
               || IsInvalidNumber(record.TotalRuntimeHours);
    }

    private static bool IsInvalidNumber(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value);
    }
}
