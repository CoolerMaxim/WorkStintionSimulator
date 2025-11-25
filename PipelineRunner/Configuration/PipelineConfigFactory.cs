using System;
using System.IO;
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
        var workingDirectory = PathNormalizer.NormalizeRelativeToWorkingDirectory(
            options.WorkingDirectory,
            AppContext.BaseDirectory);

        var normalizedSimulationSettings = NormalizeSimulationSettings(simulationSettings, workingDirectory);
        var generationOutputPath = PathNormalizer.NormalizeRelativeToWorkingDirectory(
            string.IsNullOrWhiteSpace(options.OutputPath) ? normalizedSimulationSettings.OutputPath : options.OutputPath,
            workingDirectory);

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
            OutputPath = generationOutputPath,
            Seed = options.Seed
        };

        var generation = GenerationConfigFactory.Create(normalizedSimulationSettings, request);

        var datasetWorkingDirectory = PathNormalizer.NormalizeRelativeToWorkingDirectory(
            datasetSettings.WorkingDirectory,
            workingDirectory);

        var datasetOutputPath = PathNormalizer.NormalizeRelativeToWorkingDirectory(
            datasetSettings.OutputPath,
            datasetWorkingDirectory);

        var dataset = new DatasetConfig
        {
            WorkingDirectory = datasetWorkingDirectory,
            OutputPath = datasetOutputPath,
            DatasetName = string.IsNullOrWhiteSpace(datasetSettings.DatasetName)
                ? Path.GetFileNameWithoutExtension(datasetOutputPath)
                : datasetSettings.DatasetName,
            QualityMarkdownPath = PathNormalizer.NormalizeRelativeToWorkingDirectory(
                datasetSettings.QualityMarkdownPath,
                datasetWorkingDirectory),
            QualityJsonPath = PathNormalizer.NormalizeRelativeToWorkingDirectory(
                datasetSettings.QualityJsonPath,
                datasetWorkingDirectory)
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
            WorkingDirectory = workingDirectory,
            SolutionPath = options.SolutionPath,
            TelemetryProjectPath = options.TelemetryProjectPath
        };
    }

    private static SimulationSettings NormalizeSimulationSettings(
        SimulationSettings simulationSettings,
        string workingDirectory)
    {
        return new SimulationSettings
        {
            Scenario = simulationSettings.Scenario,
            Difficulty = simulationSettings.Difficulty,
            Start = simulationSettings.Start,
            Duration = simulationSettings.Duration,
            StepMinutes = simulationSettings.StepMinutes,
            WorkstationId = simulationSettings.WorkstationId,
            SpeakersConfigured = simulationSettings.SpeakersConfigured,
            NodeProfile = simulationSettings.NodeProfile,
            OutputPath = PathNormalizer.NormalizeRelativeToWorkingDirectory(
                simulationSettings.OutputPath,
                workingDirectory),
            Seed = simulationSettings.Seed
        };
    }
}
