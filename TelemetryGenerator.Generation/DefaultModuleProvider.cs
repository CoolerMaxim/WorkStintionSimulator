using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.Modules;

namespace TelemetryGenerator.Generation;

public sealed class DefaultModuleProvider : IModuleProvider
{
    public IReadOnlyCollection<IModule> CreateModules(DifficultyProfile profile, TemperatureModel temperatureModel)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(temperatureModel);

        return new List<IModule>
        {
            new PowerSupplyModule(),
            new MicroControllerModule(),
            new AmplifierModule(),
            new EnvironmentModule(temperatureModel),
            new NetworkControllerModule(new NetworkModel(), profile)
        };
    }
}
