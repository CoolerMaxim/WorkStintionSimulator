namespace WorkstationJobSimulator.Models.wsModels;

public class Battery
{
    public BatteryStatus Status { get; set; } = BatteryStatus.Ok;
    public int ChargePercent { get; set; } = 100;
    public double Current { get; set; }
    public double Voltage { get; set; }
}
