namespace WorkstationJobSimulator.Models.wsModels;

// Нижче – допоміжні моделі, зав'язані на AppEnums

public class Amplifier
{
    public AmplifierStatus Status { get; set; } = AmplifierStatus.Off;
    public bool IsBroadcasting { get; set; }
    public double LineCurrent { get; set; }
    public double TemperatureCelsius { get; set; }
    public int LastErrorCode { get; set; }
}
