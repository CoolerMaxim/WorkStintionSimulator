using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Scenarios;

public sealed record Scenario(
    string Name,
    Difficulty Difficulty,
    IReadOnlyList<ScenarioPhase> Phases);
