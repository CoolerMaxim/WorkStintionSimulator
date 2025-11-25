namespace PipelineRunner.Services;

public interface ITrainingJob
{
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}
