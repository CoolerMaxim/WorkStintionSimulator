namespace WorkstationJobSimulator.Logging;

public interface ISimulationLogger
{
    void Log(LogLevel level, string context, string message);
}

public static class SimulationLoggerExtensions
{
    public static void LogDebug(this ISimulationLogger logger, string context, string message) =>
        logger.Log(LogLevel.Debug, context, message);

    public static void LogInformation(this ISimulationLogger logger, string context, string message) =>
        logger.Log(LogLevel.Information, context, message);

    public static void LogWarning(this ISimulationLogger logger, string context, string message) =>
        logger.Log(LogLevel.Warning, context, message);

    public static void LogError(this ISimulationLogger logger, string context, string message) =>
        logger.Log(LogLevel.Error, context, message);
}
