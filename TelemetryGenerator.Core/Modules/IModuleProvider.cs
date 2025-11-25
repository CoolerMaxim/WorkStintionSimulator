using TelemetryGenerator.Core.Models;

namespace TelemetryGenerator.Core.Modules;

public interface IModuleProvider
{
    IReadOnlyCollection<IModule> CreateModules(
        DifficultyProfile profile,
        ITemperatureModel temperatureModel,
        INetworkModel networkModel);
}
