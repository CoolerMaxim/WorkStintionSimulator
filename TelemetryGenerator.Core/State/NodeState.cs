using System.Collections.Generic;
using System.Collections.ObjectModel;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.State;

public sealed class NodeState
{
    public DateTime Timestamp { get; set; }

    public DateTime LastBootTime { get; private set; }
    public TimeSpan Uptime { get; set; }
    public TimeSpan TotalRuntime { get; set; }
    public int RebootCount { get; private set; }
    public bool IsNodeOnline { get; set; }

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

    public string FirmwareVersion { get; set; } = "FW-1.0.0";
    public string SoftwareVersion { get; set; } = "APP-1.0.0";
    public string HardwareRevision { get; set; } = "HW-1";

    public SoftwareHealthSnapshot SoftwareHealth { get; } = new();
    public FaultCounters FaultCounters { get; } = new();
    public HealthState HealthState { get; set; } = HealthState.Nominal;

    public Queue<(MaintenanceType Type, DateTime When)> PlannedMaintenance { get; } = new();

    private const int MaxIncidentEntries = 64;
    private const int MaxRestartHistory = 16;

    private readonly List<IncidentLogEntry> _incidentLog = new();
    private readonly Queue<DateTime> _restartHistory = new();

    public void RecordInitialBoot(DateTime timestamp)
    {
        IsNodeOnline = true;
        RecordRestart(timestamp, "Initial boot sequence");
        RebootCount--; // exclude initial boot from restart count
    }

    public void RecordRestart(DateTime timestamp, string reason)
    {
        RebootCount++;
        LastBootTime = timestamp;
        Uptime = TimeSpan.Zero;
        IsNodeOnline = true;

        _restartHistory.Enqueue(timestamp);
        while (_restartHistory.Count > MaxRestartHistory)
        {
            _restartHistory.Dequeue();
        }

        AddIncident("restart", reason, timestamp);
    }

    public void AddIncident(string category, string description, DateTime? timestamp = null)
    {
        category ??= "general";
        description ??= string.Empty;
        var entry = new IncidentLogEntry(timestamp ?? Timestamp, category, description);
        if (_incidentLog.Count >= MaxIncidentEntries)
        {
            _incidentLog.RemoveAt(0);
        }

        _incidentLog.Add(entry);
    }

    public IReadOnlyList<IncidentLogEntry> GetIncidentLogSnapshot()
    {
        return new ReadOnlyCollection<IncidentLogEntry>(_incidentLog.ToArray());
    }

    public IReadOnlyList<DateTime> GetRestartHistorySnapshot()
    {
        return new ReadOnlyCollection<DateTime>(_restartHistory.ToArray());
    }
}
