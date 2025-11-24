using Xunit;
using TelemetryGenerator.Core.Utilities;

namespace TelemetryGenerator.Generation.Tests;

public class DurationParserTests
{
    [Theory]
    [InlineData("1d", 24)]
    [InlineData("2.5d", 60)]
    [InlineData("3h", 3)]
    [InlineData("1.5H", 1.5)]
    [InlineData("4", 4)]
    [InlineData(" 0.5 ", 0.5)]
    public void TryParse_ReturnsTrue_ForSupportedFormats(string input, double expectedHours)
    {
        var success = DurationParser.TryParse(input, out var duration);

        Assert.True(success);
        Assert.Equal(expectedHours, duration.TotalHours, precision: 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("h")]
    [InlineData("abc")]
    [InlineData("10m")]
    [InlineData("1d2h")]
    public void TryParse_ReturnsFalse_ForInvalidValues(string input)
    {
        var success = DurationParser.TryParse(input, out var duration);

        Assert.False(success);
        Assert.Equal(TimeSpan.Zero, duration);
    }
}
