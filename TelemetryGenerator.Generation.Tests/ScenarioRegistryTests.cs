using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;
using TelemetryGenerator.Core.Scenarios;
using TelemetryGenerator.Core.Services;
using TelemetryGenerator.Generation;
using Xunit;

namespace TelemetryGenerator.Generation.Tests;

public class ScenarioRegistryTests
{
    private static AnomalyInjector CreateInjector() => new(AnomalyConfigurationFactory.CreateDefault());

    [Fact]
    public void Resolve_ReturnsScenario_WhenRegistered()
    {
        var registry = new ScenarioRegistry();
        registry.Register("normal", (difficulty, _) => new Scenario("NormalDay", difficulty, Array.Empty<ScenarioPhase>()));

        var scenario = registry.Resolve("Normal", Difficulty.Easy, CreateInjector());

        Assert.Equal("NormalDay", scenario.Name);
        Assert.Equal(Difficulty.Easy, scenario.Difficulty);
    }

    [Fact]
    public void TryResolve_ReturnsFalse_ForUnknownScenario()
    {
        var registry = new ScenarioRegistry();

        var found = registry.TryResolve("missing", Difficulty.Normal, CreateInjector(), out var scenario);

        Assert.False(found);
        Assert.Null(scenario);
    }

    [Fact]
    public void Resolve_Throws_ForUnknownScenario()
    {
        var registry = new ScenarioRegistry();

        Assert.Throws<ArgumentOutOfRangeException>(() => registry.Resolve("missing", Difficulty.Hard, CreateInjector()));
    }
}
