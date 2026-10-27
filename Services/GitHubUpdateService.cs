using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace HappyColorSplit.Services;

public sealed record AvailableUpdate(long ReleaseId, string Tag, DateTimeOffset PublishedAt, Uri Page);
public enum UpdateCheckState { UpToDate, Available, Unavailable, Untrusted }
public sealed record UpdateCheckResult(UpdateCheckState State, AvailableUpdate? Update = null);
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(string currentVersion, CancellationToken token);
}

public sealed class GitHubUpdateService(HttpClient? http = null) : IUpdateService
{
    // Pinned from GitHub's public API on 2026-10-27. Names alone must never establish trust.
    public const long RepositoryId = 1426805595, OwnerId = 330600945, PublisherId = 8838505;
    public const string Repository = "HappyProjectLab/HappyColorSplit";
    private const string ApiRoot = "https://api.github.com/repos/" + Repository;
    private const int MaxResponseBytes = 2 * 1024 * 1024;
    private static readonly HttpClient SharedHttp = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(10) };
    private readonly HttpClient _http = http ?? SharedHttp;
    public static Uri ReleasePage(string tag) => new($"https://github.com/{Repository}/releases/tag/{Uri.EscapeDataString(tag)}");
    public static bool IsReleasePage(Uri page)
    {
        string prefix = $"https://github.com/{Repository}/releases/tag/";
        if (!page.IsAbsoluteUri || !page.AbsoluteUri.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string tag = Uri.UnescapeDataString(page.AbsoluteUri[prefix.Length..]);
        return ReleaseVersion.TryParse(tag, out _) && page.AbsoluteUri == ReleasePage(tag).AbsoluteUri;
    }

    public async Task<UpdateCheckResult> CheckAsync(string currentVersion, CancellationToken token)
    {
        if (!ReleaseVersion.TryParse(currentVersion, out var current)) return new(UpdateCheckState.Unavailable);
        try
        {
            using var repoDocument = await GetJsonAsync(ApiRoot, token).ConfigureAwait(false);
            var repo = repoDocument.RootElement;
            if (repo.GetProperty("id").GetInt64() != RepositoryId || repo.GetProperty("owner").GetProperty("id").GetInt64() != OwnerId ||
                repo.GetProperty("full_name").GetString() != Repository || repo.GetProperty("html_url").GetString() != $"https://github.com/{Repository}")
                return new(UpdateCheckState.Untrusted);

            using var releasesDocument = await GetJsonAsync(ApiRoot + "/releases?per_page=100", token).ConfigureAwait(false);
            AvailableUpdate? best = null;
            var bestVersion = current;
            bool untrusted = false;
            foreach (var release in releasesDocument.RootElement.EnumerateArray())
            {
                if (release.GetProperty("draft").GetBoolean()) continue;
                string? tag = release.GetProperty("tag_name").GetString();
                if (!ReleaseVersion.TryParse(tag, out var version) || version.CompareTo(current) <= 0) continue;
                // Stable installations stay on stable releases; existing previews also see newer previews.
                if (!current.IsPrerelease && (version.IsPrerelease || release.GetProperty("prerelease").GetBoolean())) continue;
                if (release.GetProperty("author").GetProperty("id").GetInt64() != PublisherId ||
                    release.GetProperty("html_url").GetString() != ReleasePage(tag!).AbsoluteUri)
                { untrusted = true; continue; }
                if (release.GetProperty("published_at").ValueKind != JsonValueKind.String ||
                    !release.GetProperty("published_at").TryGetDateTimeOffset(out var published) || release.GetProperty("id").GetInt64() <= 0) continue;
                if (version.CompareTo(bestVersion) <= 0) continue;
                bestVersion = version;
                best = new(release.GetProperty("id").GetInt64(), tag!, published, ReleasePage(tag!));
            }
            return best is not null ? new(UpdateCheckState.Available, best) : new(untrusted ? UpdateCheckState.Untrusted : UpdateCheckState.UpToDate);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or OperationCanceledException or JsonException or
            KeyNotFoundException or InvalidOperationException or FormatException or OverflowException or ArgumentException)
        { return new(UpdateCheckState.Unavailable); }
    }

    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("HappyColorSplit/" + AppVersion.Current);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK || response.RequestMessage?.RequestUri != request.RequestUri)
            throw new HttpRequestException("Unexpected GitHub response or redirect.");
        if (response.Content.Headers.ContentLength > MaxResponseBytes) throw new IOException("Response too large.");
        using var content = new MemoryStream();
        using var source = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        byte[] buffer = new byte[16384];
        int count;
        while ((count = await source.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) != 0)
        {
            if (content.Length + count > MaxResponseBytes) throw new IOException("Response too large.");
            content.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(content.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
}
