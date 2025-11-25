using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Configuration;

/// <summary>
/// Canonical typed configuration for a simulation run.
/// </summary>
public sealed class SimulationConfig
{
    public string Scenario { get; init; } = TelemetryDefaults.Scenario;

    public Difficulty Difficulty { get; init; } = TelemetryDefaults.DefaultDifficulty;

    public DateTime Start { get; init; } = TelemetryDefaults.Start;

    public TimeSpan Duration { get; init; } = TelemetryDefaults.Duration;

    public TimeSpan Step { get; init; } = TelemetryDefaults.Step;

    public string WorkstationId { get; init; } = TelemetryDefaults.WorkstationId;

    public int SpeakersConfigured { get; init; } = TelemetryDefaults.SpeakersConfigured;

    public string NodeProfile { get; init; } = TelemetryDefaults.NodeProfile;
}
