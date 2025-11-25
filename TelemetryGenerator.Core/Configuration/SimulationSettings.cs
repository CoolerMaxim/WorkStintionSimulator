namespace TelemetryGenerator.Core.Configuration;

public sealed class SimulationSettings
{
    public string Scenario { get; set; } = "normal-day";

    public string Difficulty { get; set; } = "Normal";

    public string Start { get; set; } = string.Empty;

    public string Duration { get; set; } = "24h";

    public int StepMinutes { get; set; } = 5;

    public string WorkstationId { get; set; } = "WS-001";

    public int SpeakersConfigured { get; set; } = 4;

    public string NodeProfile { get; set; } = "randomized";

    public string OutputPath { get; set; } = "./telemetry.csv";

    public int? Seed { get; set; }
}
