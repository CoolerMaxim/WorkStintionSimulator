using AI.Training.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AI.Training.Data;

public class DatasetLoader
{
    private readonly MLContext _mlContext;

    public DatasetLoader(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    public IEnumerable<TelemetryRecord> Load(string csvPath)
    {
        var loader = _mlContext.Data.CreateTextLoader(new TextLoader.Options
        {
            Separators = new[] { ',' },
            HasHeader = true,
            Columns = new[]
            {
                new TextLoader.Column(nameof(TelemetryRecord.Timestamp), DataKind.String, 0),
                new TextLoader.Column(nameof(TelemetryRecord.WorkStationId), DataKind.String, 1),
                new TextLoader.Column(nameof(TelemetryRecord.BatteryVoltage), DataKind.Single, 4),
                new TextLoader.Column(nameof(TelemetryRecord.CpuTemperature), DataKind.Single, 5),
                new TextLoader.Column(nameof(TelemetryRecord.InsideTemperature), DataKind.Single, 6),
                new TextLoader.Column(nameof(TelemetryRecord.AmplifierOutPower), DataKind.Single, 10),
                new TextLoader.Column(nameof(TelemetryRecord.NetworkLatencyMs), DataKind.Single, 13),
                new TextLoader.Column(nameof(TelemetryRecord.SpeakersConfigured), DataKind.Single, 14),
                new TextLoader.Column(nameof(TelemetryRecord.IsAnomaly), DataKind.Boolean, 15),
                new TextLoader.Column(nameof(TelemetryRecord.NodeUptimeMinutes), DataKind.Single, 17),
                new TextLoader.Column(nameof(TelemetryRecord.TotalRuntimeHours), DataKind.Single, 18),
                new TextLoader.Column(nameof(TelemetryRecord.RestartHistory), DataKind.String, 20),
                new TextLoader.Column(nameof(TelemetryRecord.HealthState), DataKind.String, 26)
            }
        });

        var data = loader.Load(csvPath);
        var records = _mlContext.Data.CreateEnumerable<TelemetryRecord>(data, reuseRowObject: false)
            .Where(r => !string.IsNullOrWhiteSpace(r.HealthState))
            .ToList();

        return records;
    }

    public (IReadOnlyList<TelemetryRecord> Train, IReadOnlyList<TelemetryRecord> Test) TemporalSplit(IEnumerable<TelemetryRecord> records)
    {
        var ordered = records
            .OrderBy(r => DateTime.Parse(r.Timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal))
            .ToList();

        var cutoff = (int)(ordered.Count * 0.7);
        var train = ordered.Take(cutoff).ToList();
        var test = ordered.Skip(cutoff).ToList();
        return (train, test);
    }
}
