using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PipelineRunner.Options;
using AI.Training.Pipeline;

namespace PipelineRunner.Steps;

internal sealed class TrainStep
{
    private readonly ILogger<TrainStep> _logger;
    private readonly PipelineOptions _options;

    public TrainStep(IOptions<PipelineOptions> options, ILogger<TrainStep> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Training model with csv={Csv} outputDir={Output}",
            _options.OutputPath,
            _options.TrainingOutput);

        Directory.CreateDirectory(_options.TrainingOutput);
        var pipeline = new TrainingPipeline();
        var (report, modelPath, metadataPath) = pipeline.Run(_options.OutputPath, _options.TrainingOutput);

        _logger.LogInformation(
            "Metrics: MacroF1={MacroF1:F3}, CriticalF1={CriticalF1:F3}, FailedRecall={FailedRecall:F3}",
            report.ModelMetrics.MacroF1,
            report.ModelMetrics.CriticalF1,
            report.ModelMetrics.FailedRecall);
        _logger.LogInformation("Model output at {Model}; metadata at {Metadata}", modelPath, metadataPath);

        return Task.FromResult(0);
    }
}
