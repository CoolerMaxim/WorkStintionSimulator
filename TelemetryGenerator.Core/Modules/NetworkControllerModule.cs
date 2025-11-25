using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public sealed class NetworkControllerModule : IModule
{    
    private readonly INetworkModel _networkModel;
    private readonly DifficultyProfile _profile;

    public NetworkControllerModule(INetworkModel networkModel, DifficultyProfile profile)
    {        
        _networkModel = networkModel;
        _profile = profile;
    }

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd)
    {
        _networkModel.Update(state, _profile, rnd);
    }
}
