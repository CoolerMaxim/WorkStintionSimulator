using TelemetryGenerator.Core.Models;

namespace TelemetryGenerator.Core.Services;

public interface IBatteryModelFactory
{
    BatteryModel Create();
}
