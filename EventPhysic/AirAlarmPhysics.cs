using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

public class AirAlarmPhysics : IEventPhysics
{
    public Type EventType => typeof(AirAlarm);

    public void Apply(Workstation workstation, SimulationEvent simulationEvent)
    {
        var airAlarm = (AirAlarm)simulationEvent;

        workstation.Log("=== Фізика: повітряна тривога ===");
        workstation.SetAirAlarm(true, "Подія: повітряна тривога");
        workstation.Log($"Очікуємо завершення тривоги (тривалість: {airAlarm.Duration}).");
        workstation.SetAirAlarm(false, "Кінець повітряної тривоги");
    }
}
