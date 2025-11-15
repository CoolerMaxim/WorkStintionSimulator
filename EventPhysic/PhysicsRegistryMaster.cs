namespace WorkstationJobSimulator.EventPhysic;

public static class PhysicsRegistry
{
    public static void RegisterAllEventPhysics(WorkstationPhysicsEngine engine)
    {
        engine.Register(new AirAlarmPhysics());
        engine.Register(new TurningOffTheLightsPhysics());
    }
}
