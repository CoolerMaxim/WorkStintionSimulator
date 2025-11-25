namespace PipelineRunner.Options;

public sealed class ValidationSettings
{
    public bool EnableStructureAnalyzer { get; init; } = true;

    public bool EnableTimeGridAnalyzer { get; init; } = true;

    public bool EnablePhysicsAnalyzer { get; init; } = true;

    public bool EnableAnomalyAnalyzer { get; init; } = true;

    public bool EnableScenarioAnalyzer { get; init; } = true;

    public bool EnableMlFitnessAnalyzer { get; init; } = true;
}
