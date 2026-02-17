using PhotoRenamer.App.Services;
using Xunit;

namespace PhotoRenamer.App.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    public void TryParseTagVersion_ValidTag_ParsesVersion(string tag, int major, int minor, int patch)
    {
        var parsed = UpdateService.TryParseTagVersion(tag, out var version);

        Assert.True(parsed);
        Assert.Equal(new Version(major, minor, patch), version);
    }

    [Theory]
    [InlineData("v1.2")]
    [InlineData("v1.2.3-beta")]
    [InlineData("")]
    public void TryParseTagVersion_InvalidTag_ReturnsFalse(string tag)
    {
        var parsed = UpdateService.TryParseTagVersion(tag, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void IsUpdateAvailable_WhenLatestGreater_ReturnsTrue()
    {
        var result = UpdateService.IsUpdateAvailable(new Version(1, 2, 3), new Version(1, 2, 4));

        Assert.True(result);
    }
}
