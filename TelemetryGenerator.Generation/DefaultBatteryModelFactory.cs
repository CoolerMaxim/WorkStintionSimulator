using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class DefaultBatteryModelFactory : IBatteryModelFactory
{
    public IBatteryModel Create()
    {
        return new BatteryModel();
    }
}
