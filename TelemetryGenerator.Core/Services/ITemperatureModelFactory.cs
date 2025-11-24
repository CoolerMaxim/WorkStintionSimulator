using TelemetryGenerator.Core.Models;

namespace TelemetryGenerator.Core.Services;

public interface ITemperatureModelFactory
{
    TemperatureModel Create();
}
