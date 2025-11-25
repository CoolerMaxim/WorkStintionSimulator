using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public sealed class BatteryModel : IBatteryModel
{
    public void Update(
        NodeState state,
        NodeConfig config,
        TimeSpan dt,
        double loadCurrentA,
        bool gridAvailable,
        bool isChargingFromGrid)
    {
        var dtHours = Math.Max(dt.TotalHours, 0);
        var capacityEff = Math.Max(state.BatteryCapacityAhEff, 0.1);
        var charge = state.BatteryChargeAh;

        if (gridAvailable && isChargingFromGrid)
        {
            charge += config.GridChargeCurrentA * dtHours;
        }
        else
        {
            var peukertFactor = 1.0 + Math.Max(loadCurrentA - 1.0, 0) * 0.15;
            charge -= loadCurrentA * peukertFactor * dtHours;
        }

        charge = Math.Clamp(charge, 0, capacityEff);
        state.BatteryChargeAh = charge;

        var soc = capacityEff <= 0 ? 0 : charge / capacityEff;
        state.BatteryVoltage = ComputeVoltageFromSoc(soc, config);
        state.BatteryStatusOk = state.BatteryVoltage > config.BatteryCutoffVoltage + 0.05;

        if (state.BatteryVoltage <= config.BatteryCutoffVoltage)
        {
            state.BatteryStatusOk = false;
        }
    }

    private static double ComputeVoltageFromSoc(double soc, NodeConfig config)
    {
        soc = Math.Clamp(soc, 0, 1);
        if (soc >= 0.6)
        {
            var t = (soc - 0.6) / 0.4;
            return Lerp(25.5, config.BatteryFullVoltage, t);
        }

        if (soc >= 0.2)
        {
            var t = (soc - 0.2) / 0.4;
            return Lerp(23.5, 25.5, t);
        }

        return Lerp(config.BatteryCutoffVoltage, 23.5, soc / 0.2);
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * Math.Clamp(t, 0, 1);
    }
}
