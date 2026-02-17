using PhotoRenamer.App.Models;
using Xunit;

namespace PhotoRenamer.App.Tests;

public sealed class FolderNodeTests
{
    [Fact]
    public void Name_WithNestedPath_ReturnsLeafDirectoryName()
    {
        var node = new FolderNode { FullPath = Path.Combine("home", "photos", "vacation") };

        Assert.Equal("vacation", node.Name);
    }

    [Fact]
    public void Name_WithTrailingDirectorySeparator_ReturnsLeafDirectoryName()
    {
        var fullPath = Path.Combine("home", "photos", "vacation") + Path.DirectorySeparatorChar;
        var node = new FolderNode { FullPath = fullPath };

        Assert.Equal("vacation", node.Name);
    }
}
