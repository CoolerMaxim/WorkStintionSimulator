using AI.Training.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;

namespace PipelineRunner.Services;

internal sealed class TrainingJob : ITrainingJob
{
    private readonly ILogger<TrainingJob> _logger;
    private readonly DatasetSettings _datasetSettings;
    private readonly TrainingSettings _trainingSettings;

    public TrainingJob(
        IOptions<DatasetSettings> datasetOptions,
        IOptions<TrainingSettings> trainingOptions,
        ILogger<TrainingJob> logger)
    {
        _logger = logger;
        _datasetSettings = datasetOptions.Value;
        _trainingSettings = trainingOptions.Value;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Training model with csv={Csv} outputDir={Output} modelType={ModelType} trainFraction={TrainFraction} seed={Seed}",
            _datasetSettings.ResolvedOutputPath,
            _trainingSettings.ResolvedOutputDirectory,
            _trainingSettings.ModelType,
            _trainingSettings.TrainFraction,
            _trainingSettings.Seed);

        Directory.CreateDirectory(_trainingSettings.ResolvedOutputDirectory);

        var pipelineOptions = _trainingSettings.ToPipelineOptions();
        var pipeline = new TrainingPipeline(pipelineOptions);
        var (report, modelPath, metadataPath) = pipeline.Run(
            _datasetSettings.ResolvedOutputPath,
            _trainingSettings.ResolvedOutputDirectory);

        _logger.LogInformation(
            "Metrics: MacroF1={MacroF1:F3}, CriticalF1={CriticalF1:F3}, FailedRecall={FailedRecall:F3}",
            report.ModelMetrics.MacroF1,
            report.ModelMetrics.CriticalF1,
            report.ModelMetrics.FailedRecall);
        _logger.LogInformation("Model output at {Model}; metadata at {Metadata}", modelPath, metadataPath);

        return Task.FromResult(0);
    }
}
