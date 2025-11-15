using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

// TurningOffTheLightsPhysics.cs
public class TurningOffTheLightsPhysics : IEventPhysics
{
    public Type EventType => typeof(TurningOffTheLights);

    public void Apply(Workstation ws, SimulationEvent ev)
    {
        var off = (TurningOffTheLights)ev;

        ws.Log("=== Фізика: відключення світла ===");
        ws.SetPower(false, "Подія: відключення світла");

        // "Фізика" для основного відключення:
        // ws.ConsumeEnergy(basePower, off.Duration, "Робота під час відключення світла");

        // Якщо під час відключення був згенерований підівент AirAlarm — проганяємо його:
        foreach (var sub in off.SubEvents)
        {
            if (sub is AirAlarm air)
            {
                ws.Log(">>> Під час відключення світла сталася повітряна тривога (SubEvent)!");
                
                ws.SetAirAlarm(true, "Тривога під час відключення світла");
                
                // вилкликати фізику тривоги
                
                ws.SetAirAlarm(false, "Кінець тривоги під час відключення");
            }
        }

        ws.SetPower(true, "Світло повернулося після відключення");
        ws.Log("=== Кінець фізики: відключення світла ===");
    }
}