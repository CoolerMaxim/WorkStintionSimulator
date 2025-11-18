using AI.Training.Models;

namespace AI.Training.Features;

public class FeatureExtractor
{
    public IReadOnlyList<FeatureVector> Extract(IEnumerable<TelemetryRecord> records)
    {
        var ordered = records
            .Select(r => new
            {
                Record = r,
                Timestamp = DateTime.Parse(r.Timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal)
            })
            .OrderBy(r => r.Timestamp)
            .ToList();

        var results = new List<FeatureVector>(ordered.Count);
        var restartHistory = new Dictionary<string, Queue<DateTime>>();

        foreach (var entry in ordered)
        {
            var record = entry.Record;
            if (!restartHistory.TryGetValue(record.WorkStationId, out var queue))
            {
                queue = new Queue<DateTime>();
                restartHistory[record.WorkStationId] = queue;
            }

            var restartRate = CalculateRestartRate(queue, entry.Timestamp);
            var label = ParseHealthState(record.HealthState);

            results.Add(new FeatureVector(
                entry.Timestamp,
                record.WorkStationId,
                record.BatteryVoltage,
                record.CpuTemperature,
                record.InsideTemperature,
                record.AmplifierOutPower,
                record.NetworkLatencyMs,
                record.SpeakersConfigured,
                record.NodeUptimeMinutes,
                record.TotalRuntimeHours,
                restartRate,
                label));
        }

        return results;
    }

    private static float CalculateRestartRate(Queue<DateTime> queue, DateTime currentTimestamp)
    {
        while (queue.Count > 0 && (currentTimestamp - queue.Peek()).TotalHours > 24)
        {
            queue.Dequeue();
        }

        var restartRate = queue.Count / 24f;
        queue.Enqueue(currentTimestamp);
        return (float)restartRate;
    }

    public static HealthState ParseHealthState(string? value)
    {
        if (Enum.TryParse<HealthState>(value, true, out var parsed))
        {
            return parsed;
        }

        return value?.Trim() switch
        {
            "0" => HealthState.Normal,
            "1" => HealthState.Degraded,
            "2" => HealthState.Critical,
            "3" => HealthState.Failed,
            _ => HealthState.Normal
        };
    }
}
