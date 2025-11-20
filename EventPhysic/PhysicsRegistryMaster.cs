using System.Reflection;
using WorkstationJobSimulator.Logging;

namespace WorkstationJobSimulator.EventPhysic;

public static class PhysicsRegistry
{
    public static void RegisterAllEventPhysics(WorkstationPhysicsEngine engine, ISimulationLogger logger)
    {
        engine.RegisterFromAssembly(Assembly.GetExecutingAssembly());

        if (engine.RegisteredPhysicsTypes.Count == 0)
        {
            logger.LogWarning(nameof(PhysicsRegistry), "Не знайдено жодного модуля фізики для реєстрації.");
        }
    }
}
