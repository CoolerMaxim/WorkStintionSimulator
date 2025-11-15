using WorkstationJobSimulator.Events;
using WorkstationJobSimulator.Models.wsModels;

namespace WorkstationJobSimulator.EventPhysic;

public interface IEventPhysics
{
    Type EventType { get; }
    void Apply(Workstation ws, SimulationEvent ev);
}
