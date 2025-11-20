using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.Workstation;

namespace WorkstationJobSimulator.EventPhysic;

public interface IEventPhysics
{
    Type EventType { get; }
    Task ApplyAsync(Workstation ws, SimulationEvent ev, CancellationToken cancellationToken);
}
