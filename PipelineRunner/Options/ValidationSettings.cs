namespace PipelineRunner.Options;

public sealed class ValidationSettings
{
    public bool EnableStructureAnalyzer { get; init; }

    public bool EnableTimeGridAnalyzer { get; init; }

    public bool EnablePhysicsAnalyzer { get; init; }

    public bool EnableAnomalyAnalyzer { get; init; }

    public bool EnableScenarioAnalyzer { get; init; }

    public bool EnableMlFitnessAnalyzer { get; init; }
}
