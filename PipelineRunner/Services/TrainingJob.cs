using AI.Training.Pipeline;
using AI.Training.Configuration;
using Microsoft.Extensions.Logging;
using PipelineRunner.Configuration;

namespace PipelineRunner.Services;

internal sealed class TrainingJob : ITrainingJob
{
    private readonly ILogger<TrainingJob> _logger;
    private readonly PipelineConfig _pipelineConfig;

    public TrainingJob(
        PipelineConfig pipelineConfig,
        ILogger<TrainingJob> logger)
    {
        _logger = logger;
        _pipelineConfig = pipelineConfig;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Training model with csv={Csv} outputDir={Output} modelType={ModelType} trainFraction={TrainFraction} seed={Seed}",
            _pipelineConfig.Dataset.OutputPath,
            _pipelineConfig.Training.OutputDirectory,
            _pipelineConfig.Training.ModelType,
            _pipelineConfig.Training.TrainFraction,
            _pipelineConfig.Training.Seed);

        Directory.CreateDirectory(_pipelineConfig.Training.OutputDirectory);

        var pipelineOptions = new TrainingPipelineOptions
        {
            TrainFraction = _pipelineConfig.Training.TrainFraction,
            EvaluationFraction = _pipelineConfig.Training.EvaluationFraction,
            Seed = _pipelineConfig.Training.Seed
        };
        var pipeline = new TrainingPipeline(pipelineOptions);
        var (report, modelPath, metadataPath) = pipeline.Run(
            _pipelineConfig.Dataset.OutputPath,
            _pipelineConfig.Training.OutputDirectory);

        _logger.LogInformation(
            "Metrics: MacroF1={MacroF1:F3}, CriticalF1={CriticalF1:F3}, FailedRecall={FailedRecall:F3}",
            report.ModelMetrics.MacroF1,
            report.ModelMetrics.CriticalF1,
            report.ModelMetrics.FailedRecall);
        _logger.LogInformation("Model output at {Model}; metadata at {Metadata}", modelPath, metadataPath);

        return Task.FromResult(0);
    }
}
