namespace WorkstationJobSimulator.Models.wsModels;

public class NetworkInterface
{
    public NetState State { get; set; } = NetState.Normal;
    public NetworkChannel ActiveChannel { get; set; } = NetworkChannel.Main;
    public int PingLatencyMs { get; set; }
    public int Retries { get; set; }
}
