namespace PipelineRunner.Options;

internal static class ApplicationPaths
{
    public static readonly string ApplicationRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
