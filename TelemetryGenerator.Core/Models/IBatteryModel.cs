using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Models;

public interface IBatteryModel
{
    void Update(
        NodeState state,
        NodeConfig config,
        TimeSpan dt,
        double loadCurrentA,
        bool gridAvailable,
        bool isChargingFromGrid);
}
