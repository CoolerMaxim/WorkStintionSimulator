namespace TelemetryGenerator.Core.Configuration;

/// <summary>
/// Describes the electrical and firmware configuration of a simulated workstation node.
/// </summary>
public sealed record NodeConfig(
    string WorkStationId,
    int SpeakersConfigured,
    double BatteryCapacityAh,
    double BatteryCutoffVoltage,
    double BatteryFullVoltage,
    double McCurrentA,
    double NetCurrentA,
    double SpeakerCurrentA,
    double GridChargeCurrentA,
    string FirmwareVersion,
    string SoftwareVersion,
    string HardwareRevision)
{
    /// <summary>
    /// Returns the canonical configuration that reflects the original hardware assumptions.
    /// </summary>
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
            GridChargeCurrentA: 3.0,
            FirmwareVersion: "FW-1.0.0",
            SoftwareVersion: "APP-1.0.0",
            HardwareRevision: "HW-1");
    }
}
