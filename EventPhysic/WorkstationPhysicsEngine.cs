using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public void ApplyPhysics(Workstation ws, SimulationEvent ev)
    {
        if (_handlers.TryGetValue(ev.GetType(), out var handler))
        {
            ws.Log($"[Engine] Обробляємо подію: {ev.EventName}");
            handler.Apply(ws, ev);
        }
        else
        {
            ws.Log($"[Engine] Немає фізики для події {ev.GetType().Name}, просто пропускаємо.");
        }
    }
}


