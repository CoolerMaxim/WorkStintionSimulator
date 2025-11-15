using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

/// <summary>
/// Модуль мікроконтролера:
/// - гарантує узгодженість SoundStatus при відсутності живлення;
/// - моделює CpuTemperature;
/// - поступово збільшує DiskSpaceUse (логи, телеметрія).
/// </summary>
public sealed class MicroControllerModule : IModule
{
    private readonly double _baseCpuTemperature;
    private readonly double _loadCpuDelta;
    private readonly double _cpuTimeConstantMinutes;
    private readonly double _baseDiskGrowthPerMinute;
    private readonly double _extraDiskGrowthWhenSoundPerMinute;

    public MicroControllerModule(
        double baseCpuTemperature = 40.0,
        double loadCpuDelta = 15.0,
        double cpuTimeConstantMinutes = 20.0,
        double baseDiskGrowthPerMinute = 0.001,
        double extraDiskGrowthWhenSoundPerMinute = 0.005)
    {
        _baseCpuTemperature = baseCpuTemperature;
        _loadCpuDelta = loadCpuDelta;
        _cpuTimeConstantMinutes = cpuTimeConstantMinutes;
        _baseDiskGrowthPerMinute = baseDiskGrowthPerMinute;
        _extraDiskGrowthWhenSoundPerMinute = extraDiskGrowthWhenSoundPerMinute;
    }

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rng)
    {
        if (!state.PowerStatus && !state.BatteryStatusOk)
        {
            state.SoundStatus = false;
        }

        UpdateCpuTemperature(state, dt, rng);
        UpdateDiskSpaceUse(state, dt, rng);
    }

    private void UpdateCpuTemperature(NodeState state, TimeSpan dt, Random rng)
    {
        var minutes = Math.Max(dt.TotalMinutes, 0.0001);
        var target = _baseCpuTemperature + (state.SoundStatus ? _loadCpuDelta : 0.0);
        var tau = _cpuTimeConstantMinutes;
        var alpha = 1.0 - Math.Exp(-minutes / tau);
        var noise = NextGaussian(rng, 0.0, 0.2);

        state.CpuTemperature = state.CpuTemperature + alpha * (target - state.CpuTemperature) + noise;
        state.CpuTemperature = Math.Clamp(state.CpuTemperature, -20.0, 120.0);
    }

    private void UpdateDiskSpaceUse(NodeState state, TimeSpan dt, Random rng)
    {
        var minutes = Math.Max(dt.TotalMinutes, 0.0);
        var growth =
            _baseDiskGrowthPerMinute * minutes +
            (state.SoundStatus ? _extraDiskGrowthWhenSoundPerMinute * minutes : 0.0);

        var noise = NextGaussian(rng, 0.0, 0.02);
        var delta = growth + noise;

        var newValue = state.DiskSpaceUsePercent + delta;
        state.DiskSpaceUsePercent = (int)Math.Round(Math.Clamp(newValue, 0.0, 100.0));
    }

    private static double NextGaussian(Random rng, double mean, double stdDev)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }
}
