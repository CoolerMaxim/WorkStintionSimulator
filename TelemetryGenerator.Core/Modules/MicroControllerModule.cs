using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public sealed class MicroControllerModule : IModule
{
    private double _transmissionTimer;

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd)
    {
        _transmissionTimer -= dt.TotalMinutes;
        if (_transmissionTimer <= 0)
        {
            var baseInterval = state.Difficulty switch
            {
                Difficulty.Easy => rnd.Next(20, 60),
                Difficulty.Normal => rnd.Next(15, 45),
                Difficulty.Hard => rnd.Next(10, 30),
                _ => rnd.Next(20, 40)
            };
            _transmissionTimer = baseInterval;
            state.SoundStatus = rnd.NextDouble() < 0.5 ? !state.SoundStatus : state.SoundStatus;
        }

        if (state.AnomalyType == AnomalyType.SpeakersLineShort)
        {
            state.SoundStatus = false;
        }

        var computeLoad = state.SoundStatus ? 0.8 : 0.3;
        state.CpuTemperature = state.InsideTemperature + 5 + 12 * computeLoad * (1 - state.CoolingEfficiency * 0.4);
        state.CpuTemperature += (rnd.NextDouble() - 0.5) * 1.5;

        var diskDelta = computeLoad * 0.2 + (rnd.NextDouble() - 0.5) * 0.1;
        state.DiskSpaceUsePercent = (int)Math.Clamp(state.DiskSpaceUsePercent + diskDelta, 0, 100);
    }
}
