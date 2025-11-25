using System;
using System.IO;

namespace PipelineRunner.Configuration;

internal static class PathNormalizer
{
    public static string NormalizeRelativeToWorkingDirectory(string path, string workingDirectory)
    {
        var basePath = string.IsNullOrWhiteSpace(workingDirectory)
            ? Environment.CurrentDirectory
            : workingDirectory;

        return string.IsNullOrWhiteSpace(path)
            ? Path.GetFullPath(basePath)
            : Path.GetFullPath(path, basePath);
    }
}
