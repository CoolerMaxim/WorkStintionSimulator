using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

public sealed class PowerSupplyModule : IModule
{
    private TimeSpan _powerLossRemaining = TimeSpan.Zero;

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rnd)
    {
        if (state.ForcedGridOutageRemaining > TimeSpan.Zero)
        {
            state.ForcedGridOutageRemaining = state.ForcedGridOutageRemaining > dt
                ? state.ForcedGridOutageRemaining - dt
                : TimeSpan.Zero;
            state.PowerStatus = false;
            state.ChargeMode = false;
            return;
        }

        if (_powerLossRemaining > TimeSpan.Zero)
        {
            _powerLossRemaining -= dt;
        }
        else if (rnd.NextDouble() < GetOutageProbability(state.Difficulty) * dt.TotalHours)
        {
            _powerLossRemaining = TimeSpan.FromMinutes(rnd.Next(10, 120));
        }

        var gridAvailable = _powerLossRemaining <= TimeSpan.Zero;
        state.PowerStatus = gridAvailable;
        state.ChargeMode = gridAvailable && state.BatteryChargeAh < state.BatteryCapacityAhEff * 0.95;

        if (!gridAvailable && state.BatteryVoltage <= config.BatteryCutoffVoltage)
        {
            state.SoundStatus = false;
        }
    }

    private static double GetOutageProbability(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => 0.01,
            Difficulty.Normal => 0.02,
            Difficulty.Hard => 0.04,
            _ => 0.01
        };
    }
}
