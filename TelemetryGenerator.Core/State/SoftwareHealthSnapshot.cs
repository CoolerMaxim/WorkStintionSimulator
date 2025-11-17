namespace TelemetryGenerator.Core.State;

public sealed class SoftwareHealthSnapshot
{
    public bool FirmwareHealthy { get; set; } = true;
    public bool ApplicationHealthy { get; set; } = true;
    public bool NetworkStackHealthy { get; set; } = true;
    public bool StorageSubsystemHealthy { get; set; } = true;

    public bool IsHealthy => FirmwareHealthy && ApplicationHealthy && NetworkStackHealthy && StorageSubsystemHealthy;

    public SoftwareHealthSnapshot Clone()
    {
        return new SoftwareHealthSnapshot
        {
            FirmwareHealthy = FirmwareHealthy,
            ApplicationHealthy = ApplicationHealthy,
            NetworkStackHealthy = NetworkStackHealthy,
            StorageSubsystemHealthy = StorageSubsystemHealthy
        };
    }

    public string ToSummaryString()
    {
        return string.Join(';', new[]
        {
            Format("firmware", FirmwareHealthy),
            Format("application", ApplicationHealthy),
            Format("network", NetworkStackHealthy),
            Format("storage", StorageSubsystemHealthy)
        });

        static string Format(string component, bool healthy) => $"{component}:{(healthy ? "ok" : "fault")}";
    }
}
