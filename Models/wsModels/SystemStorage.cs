namespace WorkstationJobSimulator.Models.wsModels;

public class SystemStorage
{
    public int TotalSpaceMb { get; set; } = 1024;
    public int UsedSpaceMb { get; set; }

    public int UsedPercent =>
        TotalSpaceMb <= 0 ? 0 : (int)Math.Round(100.0 * UsedSpaceMb / TotalSpaceMb);
}
