using Microsoft.ML.Data;

namespace AI.Training.Models;

public enum HealthState
{
    Normal = 0,
    Degraded = 1,
    Critical = 2,
    Failed = 3
}

public class TelemetryRecord
{
    [LoadColumn(0)]
    public string Timestamp { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string WorkStationId { get; set; } = string.Empty;

    [LoadColumn(4)]
    public float BatteryVoltage { get; set; }

    [LoadColumn(5)]
    public float CpuTemperature { get; set; }

    [LoadColumn(6)]
    public float InsideTemperature { get; set; }

    [LoadColumn(10)]
    public float AmplifierOutPower { get; set; }

    [LoadColumn(13)]
    public float NetworkLatencyMs { get; set; }

    [LoadColumn(14)]
    public float SpeakersConfigured { get; set; }

    [LoadColumn(15)]
    public bool IsAnomaly { get; set; }

    [LoadColumn(18)]
    [ColumnName("NodeUptimeMinutes")]
    public float NodeUptimeMinutes { get; set; }

    [LoadColumn(19)]
    [ColumnName("TotalRuntimeHours")]
    public float TotalRuntimeHours { get; set; }

    [LoadColumn(21)]
    public string RestartHistory { get; set; } = string.Empty;

    [LoadColumn(27)]
    public string HealthState { get; set; } = string.Empty;
}

public record FeatureVector(
    DateTime Timestamp,
    string DeviceId,
    float BatteryVoltage,
    float CpuTemperature,
    float InsideTemperature,
    float AmplifierOutPower,
    float NetworkLatencyMs,
    float SpeakersConfigured,
    float NodeUptimeMinutes,
    float TotalRuntimeHours,
    float RestartRate,
    HealthState Label);

public class ModelInput
{
    [VectorType(9)]
    public float[] Features { get; set; } = Array.Empty<float>();

    [ColumnName("Label")]
    public uint Label { get; set; }
}

public class ModelOutput
{
    public uint PredictedLabel { get; set; }
    public float[]? Score { get; set; }
}
