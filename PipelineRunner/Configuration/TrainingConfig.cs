namespace PipelineRunner.Configuration;

public sealed class TrainingConfig
{
    public string WorkingDirectory { get; init; } = string.Empty;

    public string OutputDirectory { get; init; } = string.Empty;

    public double TrainFraction { get; init; }

    public double EvaluationFraction { get; init; }

    public string ModelType { get; init; } = string.Empty;

    public int Seed { get; init; }
}
