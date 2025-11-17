using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.State;

public sealed class FaultCounters
{
    public int PowerFaults { get; private set; }
    public int NetworkFaults { get; private set; }
    public int AudioFaults { get; private set; }
    public int ThermalFaults { get; private set; }
    public int SensorFaults { get; private set; }
    public int MaintenanceActions { get; private set; }

    public int TotalFaults => PowerFaults + NetworkFaults + AudioFaults + ThermalFaults + SensorFaults;

    public void RecordAnomaly(AnomalyType type)
    {
        switch (type)
        {
            case AnomalyType.BatteryCutoff:
            case AnomalyType.BatteryDegradation:
                PowerFaults++;
                break;
            case AnomalyType.NetControllerFailure:
            case AnomalyType.NetDegradation:
                NetworkFaults++;
                break;
            case AnomalyType.SpeakersPartialFailure:
            case AnomalyType.SpeakersLineOpen:
            case AnomalyType.SpeakersLineShort:
                AudioFaults++;
                break;
            case AnomalyType.CoolingDegradation:
                ThermalFaults++;
                break;
            case AnomalyType.TempSensorFailure:
                SensorFaults++;
                break;
        }
    }

    public void RecordMaintenance(MaintenanceType type)
    {
        if (type != MaintenanceType.None)
        {
            MaintenanceActions++;
        }
    }

    public FaultCountersSnapshot CreateSnapshot()
    {
        return new FaultCountersSnapshot(
            PowerFaults,
            NetworkFaults,
            AudioFaults,
            ThermalFaults,
            SensorFaults,
            MaintenanceActions,
            TotalFaults);
    }
}
