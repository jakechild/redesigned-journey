using System.IO;

namespace PhotoRenamer.App.Models;

public sealed class PhotoFile
{
    public required string FullPath { get; init; }

    public string DirectoryPath => Path.GetDirectoryName(FullPath) ?? string.Empty;

    public string Extension => Path.GetExtension(FullPath);

    public string DisplayName => Path.GetFileName(FullPath);
}
