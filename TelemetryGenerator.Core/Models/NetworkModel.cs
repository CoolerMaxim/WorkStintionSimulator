using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public sealed class NetworkModel : INetworkModel
{
    public void Update(NodeState state, DifficultyProfile profile, Random rnd)
    {
        switch (state.AnomalyType)
        {
            case AnomalyType.NetControllerFailure:
                state.SignalStrengthDbm = Math.Min(state.SignalStrengthDbm, -105);
                state.NetworkLatencyMs = 9999;
                return;
            case AnomalyType.NetDegradation:
                ApplyDegradation(state, rnd);
                return;
        }

        var jitter = (rnd.NextDouble() - 0.5) * profile.SensorNoiseLevel * 5.0;
        state.SignalStrengthDbm = -75 + rnd.NextDouble() * 25 + jitter;
        state.SignalStrengthDbm = Math.Clamp(state.SignalStrengthDbm, -110, -40);
        state.NetworkLatencyMs = (int)Math.Clamp(10 + rnd.NextDouble() * 140 + profile.SensorNoiseLevel * rnd.Next(0, 40), 5, 500);
    }

    private static void ApplyDegradation(NodeState state, Random rnd)
    {
        state.SignalStrengthDbm -= rnd.NextDouble() * 1.5;
        state.SignalStrengthDbm = Math.Clamp(state.SignalStrengthDbm, -120, -50);
        var spikes = rnd.NextDouble() < 0.3 ? rnd.Next(200, 1000) : rnd.Next(100, 300);
        state.NetworkLatencyMs = spikes;
    }
}
