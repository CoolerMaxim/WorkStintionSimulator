using System.Globalization;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Services;

public sealed class TelemetryGenerationOptions
{
    public string Scenario { get; set; } = "normal-day";

    public Difficulty Difficulty { get; set; } = Difficulty.Normal;

    public DateTime Start { get; set; } = DateTime.Now;

    public TimeSpan Duration { get; set; } = TimeSpan.FromHours(24);

    public TimeSpan Step { get; set; } = TimeSpan.FromMinutes(5);

    public string WorkstationId { get; set; } = "WS-001";

    public int SpeakersConfigured { get; set; } = 4;

    public string NodeProfile { get; set; } = "randomized";

    public string OutputPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "telemetry.csv");

    public override string ToString()
    {
        return string.Join(
            ", ",
            $"scenario={Scenario}",
            $"difficulty={Difficulty}",
            $"start={Start.ToString("O", CultureInfo.InvariantCulture)}",
            $"duration={Duration}",
            $"step={Step}",
            $"workstation={WorkstationId}",
            $"speakers={SpeakersConfigured}",
            $"nodeProfile={NodeProfile}",
            $"output={OutputPath}");
    }
}

public interface ITelemetryGenerationService
{
    Task GenerateAsync(TelemetryGenerationOptions options, CancellationToken cancellationToken = default);
}
