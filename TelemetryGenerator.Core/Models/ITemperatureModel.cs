using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public interface ITemperatureModel
{
    double GetOutsideTemperature(DateTime t, Random rnd);

    void UpdateInsideTemperature(NodeState state, double loadFactor, TimeSpan dt, Random rnd);
}
