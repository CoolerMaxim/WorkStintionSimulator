namespace WorkstationJobSimulator.Models.wsModels;

public class PowerSystem
{
    public PowerMode Mode { get; set; } = default;
    public double MainVoltage { get; set; } = 220.0;
    public double BackupVoltage { get; set; } = 12.0;
    public bool IsFuseOk { get; set; } = true;
}
