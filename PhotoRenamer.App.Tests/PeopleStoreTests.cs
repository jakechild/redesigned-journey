using PhotoRenamer.App.Services;

namespace PhotoRenamer.App.Tests;

public sealed class PeopleStoreTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "PhotoRenamerTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsEmptyList()
    {
        var filePath = Path.Combine(_tempDirectory, "people.json");
        var sut = new PeopleStore(filePath);

        var people = sut.Load();

        Assert.Empty(people);
    }

    [Fact]
    public void Save_NormalizesSortsAndDeDuplicatesPeople()
    {
        var filePath = Path.Combine(_tempDirectory, "people.json");
        var sut = new PeopleStore(filePath);

        sut.Save(["  alice ", "Bob", "ALICE", "", "   ", "charlie"]);

        var people = sut.Load();

        Assert.Equal(["alice", "Bob", "charlie"], people);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
