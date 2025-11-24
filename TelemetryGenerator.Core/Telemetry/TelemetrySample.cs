using System.Collections.Generic;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Telemetry;

public sealed record TelemetrySample(
    DateTime Timestamp,
    string WorkStationId,
    bool PowerStatus,
    bool BatteryStatus,
    double BatteryVoltage,
    double CpuTemperature,
    double InsideTemperature,
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
    MaintenanceType MaintenanceType,
    TimeSpan NodeUptime,
    TimeSpan TotalRuntime,
    int RestartCount,
    IReadOnlyList<DateTime> RestartHistory,
    SoftwareHealthSnapshot SoftwareHealth,
    string FirmwareVersion,
    string SoftwareVersion,
    string HardwareRevision,
    FaultCountersSnapshot FaultCounters,
    IReadOnlyList<IncidentLogEntry> IncidentLog,
    HealthState HealthState,
    bool IsNodeOnline);
