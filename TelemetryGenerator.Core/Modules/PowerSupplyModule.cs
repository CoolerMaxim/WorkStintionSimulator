using TelemetryGenerator.Core.Configuration;
using TelemetryGenerator.Core.State;

namespace TelemetryGenerator.Core.Modules;

/// <summary>
/// Модуль блоку живлення.
/// - Використовує PowerStatus як "мережа є / немає";
/// - Визначає IsChargingFromGrid;
/// - Відмічає BatteryStatusOk = false при падінні напруги нижче порогу та вимикає споживачів.
/// </summary>
public sealed class PowerSupplyModule : IModule
{
    private readonly double _chargeStartDeltaV;
    private readonly double _chargeStopDeltaV;

    public PowerSupplyModule(
        double chargeStartDeltaV = 0.5,
        double chargeStopDeltaV = 0.1)
    {
        _chargeStartDeltaV = chargeStartDeltaV;
        _chargeStopDeltaV = chargeStopDeltaV;
    }

    public void Update(NodeState state, NodeConfig config, TimeSpan dt, Random rng)
    {
        if (state.PowerStatus)
        {
            if (state.BatteryVoltage < config.BatteryFullVoltage - _chargeStartDeltaV)
            {
                state.IsChargingFromGrid = true;
                state.BatteryStatusOk = true;
            }
            else if (state.BatteryVoltage > config.BatteryFullVoltage - _chargeStopDeltaV)
            {
                state.IsChargingFromGrid = false;
                state.BatteryStatusOk = true;
            }
        }
        else
        {
            state.IsChargingFromGrid = false;

            if (state.BatteryVoltage <= config.BatteryCutoffVoltage)
            {
                state.BatteryStatusOk = false;
                state.SoundStatus = false;
                state.AmplifierStatus = false;
            }
        }
    }
}
