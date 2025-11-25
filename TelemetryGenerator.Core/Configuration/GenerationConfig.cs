namespace TelemetryGenerator.Core.Configuration;

/// <summary>
/// Canonical typed configuration for generating telemetry output.
/// </summary>
public sealed class GenerationConfig
{
    public SimulationConfig Simulation { get; init; } = new();

    public string OutputPath { get; init; } = TelemetryDefaults.BuildOutputPath();

    public int? Seed { get; init; }

    public string Scenario => Simulation.Scenario;

    public Enums.Difficulty Difficulty => Simulation.Difficulty;

    public DateTime Start => Simulation.Start;

    public TimeSpan Duration => Simulation.Duration;

    public TimeSpan Step => Simulation.Step;

    public string WorkstationId => Simulation.WorkstationId;

    public int SpeakersConfigured => Simulation.SpeakersConfigured;

    public string NodeProfile => Simulation.NodeProfile;
}
