using System;
using System.Collections.Generic;
using System.Linq;
using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

public class WorkstationPhysicsEngine
{
    private readonly Dictionary<Type, IEventPhysics> _handlers = new();

    public void Register(IEventPhysics physics)
    {
        _handlers[physics.EventType] = physics;
    }

    public IReadOnlyCollection<Type> RegisteredPhysicsTypes => _handlers.Keys.ToArray();

    public bool HasPhysicsFor(Type eventType) => _handlers.ContainsKey(eventType);

    public IEnumerable<Type> GetUnmappedEvents(IEnumerable<Type> eventTypes)
    {
        foreach (var eventType in eventTypes)
        {
            if (!HasPhysicsFor(eventType))
            {
                yield return eventType;
            }
        }
    }

    public void ApplyPhysics(Workstation workstation, SimulationEvent simulationEvent)
    {
        workstation.BeginEventProcessing(simulationEvent.EventName);
        workstation.Log($"[Engine] Починаємо обробку події \"{simulationEvent.EventName}\"");

        ProcessEvent(workstation, simulationEvent);

        workstation.Log($"[Engine] Завершено обробку події \"{simulationEvent.EventName}\"");
        workstation.CompleteEventProcessing(simulationEvent.EventName);
    }

    private void ProcessEvent(Workstation workstation, SimulationEvent simulationEvent)
    {
        if (_handlers.TryGetValue(simulationEvent.GetType(), out var handler))
        {
            handler.Apply(workstation, simulationEvent);
        }
        else
        {
            workstation.Log($"[Engine] Немає фізики для події {simulationEvent.GetType().Name}, пропускаємо.");
        }

        if (simulationEvent.SubEvents.Count == 0)
        {
            return;
        }

        workstation.Log($"[Engine] Опрацьовуємо {simulationEvent.SubEvents.Count} підівент(и) для \"{simulationEvent.EventName}\"");
        foreach (var subEvent in simulationEvent.SubEvents)
        {
            ProcessEvent(workstation, subEvent);
        }
    }
}
