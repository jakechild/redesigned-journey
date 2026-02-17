using PhotoRenamer.App.Models;

namespace PhotoRenamer.App.Tests;

public sealed class PhotoFileTests
{
    [Fact]
    public void DirectoryPath_ReturnsDirectoryName()
    {
        var file = new PhotoFile { FullPath = Path.Combine("home", "photos", "image.jpg") };

        Assert.Equal(Path.Combine("home", "photos"), file.DirectoryPath);
    }

    [Fact]
    public void ExtensionAndDisplayName_ReturnExpectedValues()
    {
        var file = new PhotoFile { FullPath = Path.Combine("home", "photos", "image.heic") };

        Assert.Equal(".heic", file.Extension);
        Assert.Equal("image.heic", file.DisplayName);
    }
}
