using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Scenarios;

public sealed record Scenario(
    string Name,
    Difficulty Difficulty,
    IReadOnlyList<ScenarioPhase> Phases,
    bool RepeatPhasesUntilDuration = false,
    IReadOnlyList<ScenarioInsertion>? Insertions = null);
