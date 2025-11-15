using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Scenarios;

public sealed record ScenarioPhase(
    TimeSpan Duration,
    Action<NodeState, NodeConfig, DifficultyProfile, Random> ConfigurePhase);
