using AI.Training.Configuration;

namespace PipelineRunner.Options;

public sealed class TrainingSettings
{
    public string WorkingDirectory { get; set; } = ApplicationPaths.ApplicationRoot;

    public string OutputDirectory { get; set; } = "./out/training-output";

    public double TrainFraction { get; set; } = 0.7;

    public double EvaluationFraction { get; set; } = 0.5;

    public string ModelType { get; set; } = "binary";

    public int Seed { get; set; } = 7;

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
