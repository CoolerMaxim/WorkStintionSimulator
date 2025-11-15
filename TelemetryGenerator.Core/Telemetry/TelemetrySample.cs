using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Telemetry;

public sealed record TelemetrySample(
    DateTime Timestamp,
    string WorkStationId,
    bool PowerStatus,
    bool BatteryStatus,
    double BatteryVoltage,
    double CpuTemperature,
    int Temperature,
    int DiskSpaceUse,
    bool DoorOpenStatus,
    bool AmplifierStatus,
    double AmplifierOutPower,
    bool SoundStatus,
    double SignalStrength,
    int NetworkLatency,
    int SpeakersConfigured,
    bool IsAnomaly,
    AnomalyType AnomalyType,
    MaintenanceType MaintenanceType);
