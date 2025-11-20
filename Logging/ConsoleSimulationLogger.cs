using System.Globalization;

namespace WorkstationJobSimulator.Logging;

public class ConsoleSimulationLogger : ISimulationLogger
{
    private const string DefaultContext = "App";

    public void Log(LogLevel level, string context, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var prefix = $"[{timestamp}] [{level}] [{(string.IsNullOrWhiteSpace(context) ? DefaultContext : context)}]";

        Console.ForegroundColor = GetColor(level);
        Console.Write(prefix + " ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    private static ConsoleColor GetColor(LogLevel level) => level switch
    {
        LogLevel.Debug => ConsoleColor.DarkGray,
        LogLevel.Information => ConsoleColor.Gray,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };
}
