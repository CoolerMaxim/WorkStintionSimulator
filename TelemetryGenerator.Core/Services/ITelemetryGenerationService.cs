using System.Globalization;
using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Services;

public sealed class TelemetryGenerationOptions
{
    public string Scenario { get; set; } = TelemetryDefaults.Scenario;

    public Difficulty Difficulty { get; set; } = TelemetryDefaults.DefaultDifficulty;

    public DateTime Start { get; set; } = TelemetryDefaults.Start;

    public TimeSpan Duration { get; set; } = TelemetryDefaults.Duration;

    public TimeSpan Step { get; set; } = TelemetryDefaults.Step;

    public string WorkstationId { get; set; } = TelemetryDefaults.WorkstationId;

    public int SpeakersConfigured { get; set; } = TelemetryDefaults.SpeakersConfigured;

    public string NodeProfile { get; set; } = TelemetryDefaults.NodeProfile;

    public string OutputPath { get; set; } = TelemetryDefaults.BuildOutputPath();

    public int? Seed { get; set; }

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
            $"output={OutputPath}",
            $"seed={(Seed.HasValue ? Seed.Value.ToString(CultureInfo.InvariantCulture) : "unset")}");
    }
}

public interface ITelemetryGenerationService
{
    Task GenerateAsync(TelemetryGenerationOptions options, CancellationToken cancellationToken = default);
}
