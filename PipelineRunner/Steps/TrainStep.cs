using Microsoft.Extensions.Logging;
using PipelineRunner.Services;

namespace PipelineRunner.Steps;

internal sealed class TrainStep
{
    private readonly ILogger<TrainStep> _logger;
    private readonly ITrainingJob _trainingJob;

    public TrainStep(ITrainingJob trainingJob, ILogger<TrainStep> logger)
    {
        _logger = logger;
        _trainingJob = trainingJob;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting model training job");
        return _trainingJob.ExecuteAsync(cancellationToken);
    }
}
