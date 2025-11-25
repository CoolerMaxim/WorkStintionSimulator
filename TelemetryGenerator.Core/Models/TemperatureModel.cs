using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public sealed class TemperatureModel : ITemperatureModel
{
    public double GetOutsideTemperature(DateTime t, Random rnd)
    {
        var baseTemp = 12.0;
        var amplitude = 8.0;
        var timeOfDay = t.TimeOfDay.TotalHours;
        var phaseShift = -0.25; // warmest mid-afternoon
        var noise = (rnd.NextDouble() - 0.5) * 2.0;
        var dayComponent = Math.Sin(2 * Math.PI * ((timeOfDay / 24.0) + phaseShift));
        return baseTemp + amplitude * dayComponent + noise;
    }

    public void UpdateInsideTemperature(NodeState state, double loadFactor, TimeSpan dt, Random rnd)
    {
        var outside = state.OutsideTemperature;
        var cooling = Math.Clamp(state.CoolingEfficiency, 0, 1);
        var thermalLoad = 6 + 18 * loadFactor * (1 - 0.5 * cooling);
        var target = outside + thermalLoad * (1 - cooling);
        var inertia = Math.Clamp(dt.TotalMinutes / 30.0, 0, 1);
        state.InsideTemperature += (target - state.InsideTemperature) * inertia;
        var jitter = (rnd.NextDouble() - 0.5) * 0.5;
        state.InsideTemperature += jitter;
    }
}
