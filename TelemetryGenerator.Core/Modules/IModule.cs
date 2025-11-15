using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public interface IModule
{
    void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd);
}
