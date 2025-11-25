using PipelineRunner.Options;
using TelemetryGenerator.Cli;
using TelemetryGenerator.Core.Configuration;

namespace PipelineRunner.Configuration;

public static class PipelineConfigFactory
{
    public static PipelineConfig Create(
        PipelineOptions options,
        DatasetSettings datasetSettings,
        TrainingSettings trainingSettings,
        SimulationSettings simulationSettings)
    {
        var request = new TelemetryGenerationRequest
        {
            Scenario = options.Scenario,
            Difficulty = options.Difficulty,
            Start = options.Start,
            Duration = options.Duration,
            StepMinutes = options.StepMinutes,
            WorkstationId = options.WorkstationId,
            SpeakersConfigured = options.SpeakersConfigured,
            NodeProfile = options.NodeProfile,
            OutputPath = options.OutputPath,
            Seed = options.Seed
        };

        var generation = GenerationConfigFactory.Create(simulationSettings, request);

        var dataset = new DatasetConfig
        {
            WorkingDirectory = datasetSettings.ResolvedWorkingDirectory,
            OutputPath = datasetSettings.ResolvedOutputPath,
            DatasetName = datasetSettings.ResolvedDatasetName,
            QualityMarkdownPath = datasetSettings.ResolvedQualityMarkdownPath,
            QualityJsonPath = datasetSettings.ResolvedQualityJsonPath
        };

        var training = new TrainingConfig
        {
            WorkingDirectory = trainingSettings.ResolvedWorkingDirectory,
            OutputDirectory = trainingSettings.ResolvedOutputDirectory,
            TrainFraction = trainingSettings.TrainFraction,
            EvaluationFraction = trainingSettings.EvaluationFraction,
            ModelType = trainingSettings.ModelType,
            Seed = trainingSettings.Seed
        };

        return new PipelineConfig
        {
            Generation = generation,
            Dataset = dataset,
            Training = training,
            ShouldRunBuild = options.ShouldRunBuild,
            ShouldRunTelemetry = options.ShouldRunTelemetry,
            ShouldRunCheck = options.ShouldRunCheck,
            ShouldRunTrain = options.ShouldRunTrain,
            WorkingDirectory = options.WorkingDirectory,
            SolutionPath = options.SolutionPath,
            TelemetryProjectPath = options.TelemetryProjectPath
        };
    }
}
