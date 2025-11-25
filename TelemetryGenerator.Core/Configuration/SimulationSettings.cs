namespace TelemetryGenerator.Core.Configuration;

public sealed class SimulationSettings
{
    public required string Scenario { get; set; }

    public required string Difficulty { get; set; }

    public required string Start { get; set; }

    public required string Duration { get; set; }

    public int StepMinutes { get; set; }

    public required string WorkstationId { get; set; }

    public int SpeakersConfigured { get; set; }

    public required string NodeProfile { get; set; }

    public required string OutputPath { get; set; }

    public int? Seed { get; set; }
}
