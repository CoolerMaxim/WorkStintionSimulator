using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.Workstation;

namespace WorkstationJobSimulator.EventPhysic;

public class AirAlarmPhysics : IEventPhysics
{
    public Type EventType => typeof(AirAlarm);

    public async Task ApplyAsync(Workstation workstation, SimulationEvent simulationEvent, CancellationToken cancellationToken)
    {
        var airAlarm = (AirAlarm)simulationEvent;

        workstation.Log("=== Фізика: повітряна тривога ===");
        workstation.SetAirAlarm(true, "Подія: повітряна тривога");
        workstation.Log($"Очікуємо завершення тривоги (тривалість: {airAlarm.Duration}).");
        await Task.Delay(airAlarm.Duration, cancellationToken);
        workstation.SetAirAlarm(false, "Кінець повітряної тривоги");
    }
}
