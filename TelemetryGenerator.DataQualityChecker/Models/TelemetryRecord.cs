using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.DataQualityChecker.Models;

public sealed class TelemetryRecord
{
    public DateTime Timestamp { get; init; }

    public string WorkStationId { get; init; } = string.Empty;

    public bool PowerStatus { get; init; }

    public bool BatteryStatus { get; init; }

    public double BatteryVoltage { get; init; }

    public double CpuTemperature { get; init; }

    public double InsideTemperature { get; init; }

    public int DiskSpaceUse { get; init; }

    public bool DoorOpenStatus { get; init; }

    public bool AmplifierStatus { get; init; }

    public double AmplifierOutPower { get; init; }

    public bool SoundStatus { get; init; }

    public double SignalStrength { get; init; }

    public int NetworkLatency { get; init; }

    public int SpeakersConfigured { get; init; }

    public int? SpeakersEffective { get; init; }

    public bool IsAnomaly { get; init; }

    public AnomalyType AnomalyType { get; init; }

    public HealthState HealthState { get; init; }

    public string ScenarioId { get; init; } = string.Empty;

    public string Difficulty { get; init; } = string.Empty;

    public int RestartCount { get; init; }
}
