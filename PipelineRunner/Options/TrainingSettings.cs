using AI.Training.Configuration;

namespace PipelineRunner.Options;

public sealed class TrainingSettings
{
    public required string WorkingDirectory { get; set; }

    public required string OutputDirectory { get; set; }

    public double TrainFraction { get; set; }

    public double EvaluationFraction { get; set; }

    public required string ModelType { get; set; }

    public int Seed { get; set; }

    public string ResolvedWorkingDirectory => Path.GetFullPath(WorkingDirectory, ApplicationPaths.ApplicationRoot);

    public string ResolvedOutputDirectory => Path.GetFullPath(OutputDirectory, ResolvedWorkingDirectory);

    public TrainingPipelineOptions ToPipelineOptions()
    {
        return new TrainingPipelineOptions
        {
            TrainFraction = TrainFraction,
            EvaluationFraction = EvaluationFraction,
            Seed = Seed
        };
    }
}
