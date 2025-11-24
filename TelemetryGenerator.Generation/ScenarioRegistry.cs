using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Scenarios;
using TelemetryGenerator.Core.Services;

namespace TelemetryGenerator.Generation;

public sealed class ScenarioRegistry : IScenarioRegistry
{
    private readonly Dictionary<string, Func<Difficulty, AnomalyInjector, Scenario>> _factories;

    public ScenarioRegistry()
    {
        _factories = new Dictionary<string, Func<Difficulty, AnomalyInjector, Scenario>>(StringComparer.OrdinalIgnoreCase);
    }

    public void Register(string name, Func<Difficulty, AnomalyInjector, Scenario> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(factory);

        _factories[name] = factory;
    }

    public Scenario Resolve(string name, Difficulty difficulty, AnomalyInjector injector)
    {
        if (!TryResolve(name, difficulty, injector, out var scenario))
        {
            throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown scenario.");
        }

        return scenario ?? throw new InvalidOperationException($"Scenario '{name}' could not be resolved.");
    }

    public bool TryResolve(string name, Difficulty difficulty, AnomalyInjector injector, out Scenario? scenario)
    {
        ArgumentNullException.ThrowIfNull(injector);

        scenario = null!;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (!_factories.TryGetValue(name, out var factory))
        {
            return false;
        }

        scenario = factory(difficulty, injector) ?? throw new InvalidOperationException($"Scenario factory for '{name}' returned null.");
        return true;
    }
}
