using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Scenarios;

namespace TelemetryGenerator.Core.Services;

public interface IScenarioRegistry
{
    Scenario Resolve(string name, Difficulty difficulty, AnomalyInjector injector);

    bool TryResolve(string name, Difficulty difficulty, AnomalyInjector injector, out Scenario? scenario);

    void Register(string name, Func<Difficulty, AnomalyInjector, Scenario> factory);
}
