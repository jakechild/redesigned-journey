using System.Collections.ObjectModel;
using System.IO;

namespace PhotoRenamer.App.Models;

public sealed class FolderNode
{
    public required string FullPath { get; init; }

    public string Name
    {
        get
        {
            var directoryName = Path.GetFileName(FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(directoryName) ? FullPath : directoryName;
        }
    }

    public ObservableCollection<FolderNode> Children { get; } = [];
}
