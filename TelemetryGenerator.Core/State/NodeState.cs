using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.State;

public sealed class NodeState
{
    public DateTime Timestamp { get; set; }

    public double BatteryChargeAh { get; set; }
    public double BatteryVoltage { get; set; }
    public bool BatteryStatusOk { get; set; }
    public double BatteryCapacityAhEff { get; set; }

    public bool PowerStatus { get; set; }
    public bool IsChargingFromGrid { get; set; }
    public TimeSpan ForcedGridOutageRemaining { get; set; }

    public bool SoundStatus { get; set; }
    public bool AmplifierStatus { get; set; }
    public double AmplifierOutCurrentA { get; set; }
    public int SpeakersEffective { get; set; }

    public double CpuTemperature { get; set; }
    public double InsideTemperature { get; set; }
    public double OutsideTemperature { get; set; }
    public double CoolingEfficiency { get; set; }

    public double SignalStrengthDbm { get; set; }
    public int NetworkLatencyMs { get; set; }

    public int DiskSpaceUsePercent { get; set; }
    public bool DoorOpen { get; set; }

    public bool IsAnomaly { get; set; }
    public AnomalyType AnomalyType { get; set; }
    public MaintenanceType MaintenanceType { get; set; }

    public Difficulty Difficulty { get; set; }

    public Queue<(MaintenanceType Type, DateTime When)> PlannedMaintenance { get; } = new();
}
