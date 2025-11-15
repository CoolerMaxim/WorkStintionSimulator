using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public sealed class AmplifierModule : IModule
{
    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd)
    {
        var effectiveSpeakers = Math.Clamp(state.SpeakersEffective, 0, config.SpeakersConfigured);

        if (state.AnomalyType == AnomalyType.SpeakersLineOpen)
        {
            effectiveSpeakers = 0;
        }

        if (state.SoundStatus && effectiveSpeakers > 0 && state.BatteryStatusOk)
        {
            state.AmplifierStatus = true;
            state.AmplifierOutCurrentA = effectiveSpeakers * config.SpeakerCurrentA;
        }
        else
        {
            state.AmplifierStatus = false;
            state.AmplifierOutCurrentA = 0;
        }
    }
}
