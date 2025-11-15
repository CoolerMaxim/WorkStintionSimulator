using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public sealed class EnvironmentModule : IModule
{
    private TimeSpan _doorOpenRemaining = TimeSpan.Zero;

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd)
    {
        if (_doorOpenRemaining > TimeSpan.Zero)
        {
            _doorOpenRemaining -= dt;
        }
        else if (rnd.NextDouble() < 0.01 * dt.TotalHours)
        {
            _doorOpenRemaining = TimeSpan.FromMinutes(rnd.Next(1, 15));
        }

        state.DoorOpen = _doorOpenRemaining > TimeSpan.Zero;
    }
}
