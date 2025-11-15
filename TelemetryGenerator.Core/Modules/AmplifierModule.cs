using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

/// <summary>
/// Модуль підсилювача.
/// - Якщо SoundStatus = true → AmplifierStatus = true і струм ≈ SpeakersEffective * SpeakerCurrentA.
/// - Якщо SoundStatus = false або немає живлення → струм ≈ 0, підсилювач вимкнений.
/// </summary>
public sealed class AmplifierModule : IModule
{
    private readonly double _currentNoiseRelativeStd;

    public AmplifierModule(double currentNoiseRelativeStd = 0.05)
    {
        _currentNoiseRelativeStd = currentNoiseRelativeStd;
    }

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rng)
    {
        if (!state.PowerStatus && !state.BatteryStatusOk)
        {
            state.AmplifierStatus = false;
            state.AmplifierOutCurrentA = 0.0;
            return;
        }

        if (!state.SoundStatus)
        {
            state.AmplifierStatus = false;
            state.AmplifierOutCurrentA = 0.0;
            return;
        }

        state.AmplifierStatus = true;

        var effectiveSpeakers = Math.Clamp(state.SpeakersEffective, 0, config.SpeakersConfigured);
        var nominalCurrent = effectiveSpeakers * config.SpeakerCurrentA;
        var noiseStd = nominalCurrent * _currentNoiseRelativeStd;
        var noise = nominalCurrent > 0 ? NextGaussian(rng, 0.0, noiseStd) : 0.0;
        var current = Math.Max(0.0, nominalCurrent + noise);

        state.AmplifierOutCurrentA = current;
    }

    private static double NextGaussian(Random rng, double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}
