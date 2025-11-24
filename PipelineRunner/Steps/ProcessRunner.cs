using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PipelineRunner.Steps;

internal static class ProcessRunner
{
    public static int Run(string fileName, IEnumerable<string> arguments, string workingDirectory, ILogger logger)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        logger.LogInformation("Running {FileName} {Args}", fileName, string.Join(' ', arguments));
        process.Start();
        process.WaitForExit();
        return process.ExitCode;
    }
}
