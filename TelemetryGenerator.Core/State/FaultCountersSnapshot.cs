namespace TelemetryGenerator.Core.State;

public sealed record FaultCountersSnapshot(
    int PowerFaults,
    int NetworkFaults,
    int AudioFaults,
    int ThermalFaults,
    int SensorFaults,
    int MaintenanceActions,
    int TotalFaults)
{
    public string ToSummaryString()
    {
        return string.Join(';', new[]
        {
            $"power:{PowerFaults}",
            $"network:{NetworkFaults}",
            $"audio:{AudioFaults}",
            $"thermal:{ThermalFaults}",
            $"sensor:{SensorFaults}",
            $"maintenance:{MaintenanceActions}",
            $"total:{TotalFaults}"
        });
    }
}
