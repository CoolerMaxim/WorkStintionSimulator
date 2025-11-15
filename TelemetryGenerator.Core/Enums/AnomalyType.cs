namespace TelemetryGenerator.Core.Enums;

public enum AnomalyType
{
    None,
    BatteryCutoff,
    NetControllerFailure,
    SpeakersPartialFailure,
    SpeakersLineOpen,
    SpeakersLineShort,
    BatteryDegradation,
    NetDegradation,
    CoolingDegradation,
    TempSensorFailure
}
