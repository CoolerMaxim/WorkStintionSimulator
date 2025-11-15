using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

public class AirAlarmPhysics : IEventPhysics
{
    public Type EventType => typeof(AirAlarm);

    public void Apply(Workstation ws, SimulationEvent ev)
    {
        var air = (AirAlarm)ev;

        ws.Log("=== Фізика: повітряна тривога ===");
        ws.SetAirAlarm(true, "Подія: повітряна тривога");

        // Тут ти можеш робити "реальну фізику":
        // ws.ConsumeEnergy(basePower + extra, air.Duration, "...");

        ws.SetAirAlarm(false, "Кінець повітряної тривоги");
    }
}

