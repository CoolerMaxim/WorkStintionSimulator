namespace PipelineRunner.Services;

public interface IDataQualityJob
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
