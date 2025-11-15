using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.Models;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

/// <summary>
/// Модуль "оточення":
/// - моделює випадкові відкриття/закриття дверей;
/// - оновлює температури за допомогою TemperatureModel.
/// </summary>
public sealed class EnvironmentModule : IModule
{
    private readonly TemperatureModel _temperatureModel;
    private readonly double _doorOpenProbabilityPerHour;
    private readonly double _doorCloseProbabilityPerHour;

    public EnvironmentModule(
        TemperatureModel temperatureModel,
        double doorOpenProbabilityPerHour = 0.02,
        double doorCloseProbabilityPerHour = 0.3)
    {
        _temperatureModel = temperatureModel ?? throw new ArgumentNullException(nameof(temperatureModel));
        _doorOpenProbabilityPerHour = doorOpenProbabilityPerHour;
        _doorCloseProbabilityPerHour = doorCloseProbabilityPerHour;
    }

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rng)
    {
        UpdateDoorState(state, dt, rng);
        UpdateTemperatures(state, config, dt, rng);
    }

    private void UpdateDoorState(NodeState state, TimeSpan dt, Random rng)
    {
        var hours = Math.Max(dt.TotalHours, 0.0);

        if (!state.DoorOpen)
        {
            var pOpen = _doorOpenProbabilityPerHour * hours;
            if (rng.NextDouble() < pOpen)
            {
                state.DoorOpen = true;
            }
        }
        else
        {
            var pClose = _doorCloseProbabilityPerHour * hours;
            if (rng.NextDouble() < pClose)
            {
                state.DoorOpen = false;
            }
        }
    }

    private void UpdateTemperatures(NodeState state, NodeConfig config, TimeSpan dt, Random rng)
    {
        state.OutsideTemperature = _temperatureModel.GetOutsideTemperature(state.Timestamp, rng);

        var nominalMaxCurrent = Math.Max(1.0, config.SpeakersConfigured * config.SpeakerCurrentA);
        var loadFactor = state.AmplifierOutCurrentA / nominalMaxCurrent;
        loadFactor = Math.Clamp(loadFactor, 0.0, 1.5);

        if (state.DoorOpen)
        {
            loadFactor *= 0.9;
        }

        _temperatureModel.UpdateInsideTemperature(state, loadFactor, dt, rng);
    }
}
