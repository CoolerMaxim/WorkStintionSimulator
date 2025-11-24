namespace TelemetryGenerator.Core.Services;

public interface IRandomFactory
{
    Random Create(int? seed);
}
