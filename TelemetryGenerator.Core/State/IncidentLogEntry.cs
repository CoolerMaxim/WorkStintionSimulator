namespace TelemetryGenerator.Core.State;

public sealed record IncidentLogEntry(DateTime Timestamp, string Category, string Description)
{
    public string ToSummaryString()
    {
        var safeCategory = (Category ?? string.Empty).Replace('|', '/');
        var safeDescription = (Description ?? string.Empty)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Replace('|', '/')
            .Replace(',', ';');
        return $"{Timestamp:O}|{safeCategory}|{safeDescription}";
    }
}
