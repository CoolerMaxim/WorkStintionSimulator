using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.Workstation;

namespace WorkstationJobSimulator.EventPhysic;

public class TurningOffTheLightsPhysics : IEventPhysics
{
    public Type EventType => typeof(TurningOffTheLights);

    public async Task ApplyAsync(Workstation workstation, SimulationEvent simulationEvent, CancellationToken cancellationToken)
    {
        var turningOff = (TurningOffTheLights)simulationEvent;

        workstation.Log("=== Фізика: відключення світла ===");
        workstation.SetPower(false, "Подія: відключення світла");
        workstation.Log($"Очікуємо відновлення електропостачання (тривалість: {turningOff.Duration}).");
        await Task.Delay(turningOff.Duration, cancellationToken);
        workstation.SetPower(true, "Світло повернулося після відключення");
        workstation.Log("=== Кінець фізики: відключення світла ===");
    }
}
