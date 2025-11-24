using System.Globalization;

namespace TelemetryGenerator.Core.Utilities;

public static class DurationParser
{
    public static bool TryParse(string? value, out TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            duration = TimeSpan.Zero;
            return false;
        }

        value = value.Trim();
        if (value.EndsWith("d", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var days))
        {
            duration = TimeSpan.FromDays(days);
            return true;
        }

        if (value.EndsWith("h", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
        {
            duration = TimeSpan.FromHours(hours);
            return true;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var plainHours))
        {
            duration = TimeSpan.FromHours(plainHours);
            return true;
        }

        duration = TimeSpan.Zero;
        return false;
    }
}
