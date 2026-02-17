using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PhotoRenamer.App.Services;

public sealed class UpdateService
{
    private static readonly Regex TagVersionRegex = new("^v?(\\d+)\\.(\\d+)\\.(\\d+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient;

    public UpdateService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PhotoRenamerApp/1.0");
        }
    }

    public async Task<ReleaseInfo?> GetLatestReleaseAsync(string githubRepository, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(githubRepository))
        {
            return null;
        }

        var endpoint = $"https://api.github.com/repos/{githubRepository}/releases/latest";

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        if (!payload.RootElement.TryGetProperty("tag_name", out var tagNameElement))
        {
            return null;
        }

        var tagName = tagNameElement.GetString();
        if (!TryParseTagVersion(tagName, out var version))
        {
            return null;
        }

        var htmlUrl = payload.RootElement.TryGetProperty("html_url", out var htmlUrlElement)
            ? htmlUrlElement.GetString()
            : null;

        return new ReleaseInfo(version, tagName!, htmlUrl ?? string.Empty);
    }

    internal static bool TryParseTagVersion(string? tagName, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        var match = TagVersionRegex.Match(tagName.Trim());
        if (!match.Success)
        {
            return false;
        }

        var major = int.Parse(match.Groups[1].Value);
        var minor = int.Parse(match.Groups[2].Value);
        var patch = int.Parse(match.Groups[3].Value);
        version = new Version(major, minor, patch);
        return true;
    }

    internal static bool IsUpdateAvailable(Version currentVersion, Version latestVersion)
    {
        return latestVersion > currentVersion;
    }
}

public sealed record ReleaseInfo(Version Version, string TagName, string HtmlUrl);
