using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultBatteryModelFactory : IBatteryModelFactory
{
    public BatteryModel Create()
    {
        return new BatteryModel();
    }
}
