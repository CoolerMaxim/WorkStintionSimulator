using System;
using TelemetryGenerator.Core.Enums;

namespace TelemetryGenerator.Core.Configuration;

/// <summary>
/// Provides helper methods to build node configurations with intentional variability
/// so that generated datasets contain heterogeneous hardware/firmware characteristics.
/// </summary>
public static class NodeConfigurationFactory
{
    /// <summary>
    /// Produces a randomized but realistic configuration derived from the provided baseline values.
    /// </summary>
    /// <param name="workStationId">Logical identifier for the node.</param>
    /// <param name="nominalSpeakers">Baseline number of speakers supplied by the caller.</param>
    /// <param name="difficulty">Difficulty level; higher difficulty broadens the variation.</param>
    /// <param name="rnd">Random source for reproducibility.</param>
    public static NodeConfig CreateRandomized(string workStationId, int nominalSpeakers, Difficulty difficulty, Random rnd)
    {
        var variationFactor = difficulty switch
        {
            Difficulty.Easy => 0.05,
            Difficulty.Normal => 0.12,
            Difficulty.Hard => 0.2,
            _ => 0.1
        };

        double Spread(double value) => value * (1 + (rnd.NextDouble() * 2 - 1) * variationFactor);

        var capacity = Spread(24.0).Clamp(18.0, 32.0);
        var cutoff = Spread(21.0).Clamp(20.5, 22.5);
        var fullVoltage = Spread(27.0).Clamp(26.0, 28.2);
        var mcCurrent = Spread(0.12).Clamp(0.08, 0.2);
        var netCurrent = Spread(0.12).Clamp(0.08, 0.25);
        var speakerCurrent = Spread(0.45).Clamp(0.3, 0.7);
        var chargeCurrent = Spread(3.0).Clamp(2.0, 4.0);

        var speakers = Math.Max(1, nominalSpeakers + rnd.Next(-1, 2));

        var firmwareVersion = $"FW-{1 + rnd.Next(0, 2)}.{rnd.Next(0, 9)}.{rnd.Next(0, 9)}";
        var softwareVersion = $"APP-{1 + rnd.Next(0, 2)}.{rnd.Next(0, 9)}.{rnd.Next(0, 9)}";
        var hardwareRevision = $"HW-{1 + rnd.Next(0, 3)}";

        return new NodeConfig(
            workStationId,
            speakers,
            capacity,
            cutoff,
            fullVoltage,
            mcCurrent,
            netCurrent,
            speakerCurrent,
            chargeCurrent,
            firmwareVersion,
            softwareVersion,
            hardwareRevision);
    }

    private static double Clamp(this double value, double min, double max)
    {
        return Math.Max(min, Math.Min(value, max));
    }
}
