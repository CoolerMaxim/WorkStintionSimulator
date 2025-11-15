using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

public class TurningOffTheLightsPhysics : IEventPhysics
{
    public Type EventType => typeof(TurningOffTheLights);

    public void Apply(Workstation workstation, SimulationEvent simulationEvent)
    {
        var turningOff = (TurningOffTheLights)simulationEvent;

        workstation.Log("=== Фізика: відключення світла ===");
        workstation.SetPower(false, "Подія: відключення світла");
        workstation.Log($"Очікуємо відновлення електропостачання (тривалість: {turningOff.Duration}).");
        workstation.SetPower(true, "Світло повернулося після відключення");
        workstation.Log("=== Кінець фізики: відключення світла ===");
    }
}
