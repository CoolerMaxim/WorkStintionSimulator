namespace TelemetryGenerator.Core.Configuration;

public sealed record NodeConfig(
    string WorkStationId,
    int SpeakersConfigured,
    double BatteryCapacityAh,
    double BatteryCutoffVoltage,
    double BatteryFullVoltage,
    double McCurrentA,
    double NetCurrentA,
    double SpeakerCurrentA,
    double GridChargeCurrentA)
{
    public static NodeConfig CreateDefault(string workStationId, int speakersConfigured)
    {
        return new NodeConfig(
            workStationId,
            speakersConfigured,
            BatteryCapacityAh: 26.0,
            BatteryCutoffVoltage: 21.0,
            BatteryFullVoltage: 27.5,
            McCurrentA: 0.125,
            NetCurrentA: 0.125,
            SpeakerCurrentA: 0.5,
            GridChargeCurrentA: 3.0);
    }
}
