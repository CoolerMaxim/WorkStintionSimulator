using System;

namespace WorkstationJobSimulator.Events;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventChanceAttribute : Attribute
{
    public double Chance { get; }

    public EventChanceAttribute(double chance)
    {
        if (chance <= 0)
            throw new ArgumentOutOfRangeException(nameof(chance), "Chance must be > 0.");

        Chance = chance;
    }
}
