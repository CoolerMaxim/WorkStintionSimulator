using Microsoft.Extensions.Logging;
using PipelineRunner.Services;

namespace PipelineRunner.Steps;

internal sealed class QualityCheckStep
{
    private readonly ILogger<QualityCheckStep> _logger;
    private readonly IDataQualityJob _dataQualityJob;

    public QualityCheckStep(
        IDataQualityJob dataQualityJob,
        ILogger<QualityCheckStep> logger)
    {
        _logger = logger;
        _dataQualityJob = dataQualityJob;
    }

    public Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data quality job");
        return _dataQualityJob.ExecuteAsync(cancellationToken);
    }
}
