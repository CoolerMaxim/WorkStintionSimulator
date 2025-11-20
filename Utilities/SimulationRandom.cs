namespace WorkstationJobSimulator.Utilities;

public static class SimulationRandom
{
    private static Random? _random;

    public static Random Instance => _random ??= new Random();

    public static void Reset(int? seed)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }
}
