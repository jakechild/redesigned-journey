using System.Text.Json;

namespace PhotoRenamer.App.Services;

public sealed class PeopleStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public PeopleStore()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhotoRenamer");

        Directory.CreateDirectory(appDataFolder);
        _filePath = Path.Combine(appDataFolder, "people.json");
    }

    public IReadOnlyList<string> Load()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<string>();
        }

        var content = File.ReadAllText(_filePath);
        var people = JsonSerializer.Deserialize<List<string>>(content);

        return people?.Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static p => p)
            .ToList() ?? Array.Empty<string>();
    }

    public void Save(IReadOnlyCollection<string> people)
    {
        var normalized = people
            .Where(static p => !string.IsNullOrWhiteSpace(p))
            .Select(static p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static p => p)
            .ToList();

        var json = JsonSerializer.Serialize(normalized, SerializerOptions);
        File.WriteAllText(_filePath, json);
    }
}
