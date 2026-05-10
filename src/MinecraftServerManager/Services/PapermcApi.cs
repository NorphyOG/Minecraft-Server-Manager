using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MinecraftServerManager.Services;

public sealed class PapermcApi
{
    private readonly HttpClient _http;

    public PapermcApi(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MinecraftServerManager/1.0");
    }

    public async Task<IReadOnlyList<string>> GetPaperVersionsAsync(CancellationToken ct = default)
    {
        var doc = await _http.GetFromJsonAsync<PaperProjectResponse>(
            "https://api.papermc.io/v2/projects/paper", ct);
        return doc?.Versions ?? [];
    }

    public async Task<(int Build, string JarFileName)?> GetLatestBuildAsync(string mcVersion, CancellationToken ct = default)
    {
        var buildsDoc = await _http.GetFromJsonAsync<PaperBuildsResponse>(
            $"https://api.papermc.io/v2/projects/paper/versions/{Uri.EscapeDataString(mcVersion)}/builds", ct);
        if (buildsDoc?.Builds is not { Count: > 0 })
            return null;

        var last = buildsDoc.Builds[^1];
        var jarName = last.Downloads?.Application?.Name ?? $"paper-{mcVersion}-{last.Build}.jar";
        return (last.Build, jarName);
    }

    public async Task DownloadPaperJarAsync(
        string mcVersion,
        int build,
        string downloadFileName,
        string destinationFile,
        IProgress<double>? progress,
        CancellationToken ct = default)
    {
        var url =
            $"https://api.papermc.io/v2/projects/paper/versions/{Uri.EscapeDataString(mcVersion)}/builds/{build}/downloads/{Uri.EscapeDataString(downloadFileName)}";

        await DownloadToFileAsync(url, destinationFile, progress, ct);
    }

    public async Task<IReadOnlyList<string>> GetVelocityVersionsAsync(CancellationToken ct = default)
    {
        var doc = await _http.GetFromJsonAsync<PaperProjectResponse>(
            "https://api.papermc.io/v2/projects/velocity", ct);
        return doc?.Versions ?? [];
    }

    public async Task<(int Build, string JarFileName)?> GetLatestVelocityBuildAsync(string version, CancellationToken ct = default)
    {
        var buildsDoc = await _http.GetFromJsonAsync<PaperBuildsResponse>(
            $"https://api.papermc.io/v2/projects/velocity/versions/{Uri.EscapeDataString(version)}/builds", ct);
        if (buildsDoc?.Builds is not { Count: > 0 })
            return null;

        var last = buildsDoc.Builds[^1];
        var jarName = last.Downloads?.Application?.Name ?? $"velocity-{version}-{last.Build}.jar";
        return (last.Build, jarName);
    }

    public async Task DownloadVelocityJarAsync(
        string velocityVersion,
        int build,
        string downloadFileName,
        string destinationFile,
        IProgress<double>? progress,
        CancellationToken ct = default)
    {
        var url =
            $"https://api.papermc.io/v2/projects/velocity/versions/{Uri.EscapeDataString(velocityVersion)}/builds/{build}/downloads/{Uri.EscapeDataString(downloadFileName)}";

        await DownloadToFileAsync(url, destinationFile, progress, ct);
    }

    private async Task DownloadToFileAsync(string url, string destinationFile, IProgress<double>? progress,
        CancellationToken ct)
    {
        PathsHelper.EnsureParent(destinationFile);
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        await using var fs = File.Create(destinationFile);
        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;
            if (total > 0 && progress != null)
                progress.Report(readTotal / (double)total);
        }
    }

    private sealed class PaperProjectResponse
    {
        [JsonPropertyName("versions")]
        public List<string>? Versions { get; set; }
    }

    private sealed class PaperBuildsResponse
    {
        [JsonPropertyName("builds")]
        public List<PaperBuildItem>? Builds { get; set; }
    }

    private sealed class PaperBuildItem
    {
        [JsonPropertyName("build")]
        public int Build { get; set; }

        [JsonPropertyName("downloads")]
        public PaperDownloads? Downloads { get; set; }
    }

    private sealed class PaperDownloads
    {
        [JsonPropertyName("application")]
        public PaperDownloadApplication? Application { get; set; }
    }

    private sealed class PaperDownloadApplication
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
