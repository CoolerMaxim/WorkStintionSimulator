using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public interface INetworkModel
{
    void Update(NodeState state, DifficultyProfile profile, Random rnd);
}
