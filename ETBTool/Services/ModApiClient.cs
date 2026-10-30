using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Services;

public class ModApiClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private string? _token;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public ModApiClient(string? baseUrl = null, HttpMessageHandler? handler = null)
    {
        _http = handler == null ? new HttpClient() : new HttpClient(handler, false);
        _http.Timeout = TimeSpan.FromSeconds(300);
        _baseUrl = (baseUrl ?? GamePaths.CatalogueUrl).TrimEnd('/');
    }

    private string EndpointUrl(string endpoint)
    {
        var parts = endpoint.TrimStart('/').Split('?', 2);
        return _baseUrl + "/api/index.php?route=" + Uri.EscapeDataString(parts[0]) +
            (parts.Length > 1 ? "&" + parts[1] : "");
    }

    public void SetToken(string? token) => _token = token;
    public string? GetToken() => _token;

    private void ApplyAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(_token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, EndpointUrl(endpoint));
        ApplyAuth(req);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch (JsonException ex) { Logger.Log($"JSON ({endpoint}): {ex.Message}"); return default; }
    }

    public async Task<T?> PostJsonAsync<T>(string endpoint, object body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, EndpointUrl(endpoint));
        ApplyAuth(req);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        try { return JsonSerializer.Deserialize<T>(json, JsonOpts); }
        catch (JsonException ex) { Logger.Log($"JSON ({endpoint}): {ex.Message}"); return default; }
    }

    public async Task<string?> GetImageUrlAsync(int imageId, CancellationToken ct = default)
    {
        var result = await GetAsync<DownloadLinkResponse>($"/images/{imageId}/url");
        return string.IsNullOrWhiteSpace(result?.Url) ? null : result!.Url;
    }

    public async Task<string?> GetDownloadUrlAsync(int modId, string? mode = null, CancellationToken ct = default)
    {
        var path = $"mods/{modId}/download" + (string.IsNullOrWhiteSpace(mode) ? "" : "?mode=" + Uri.EscapeDataString(mode));
        var result = await GetAsync<DownloadLinkResponse>(path);
        return string.IsNullOrWhiteSpace(result?.Url) ? null : result!.Url;
    }

    public async Task<byte[]?> DownloadImageBytesAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return null;
        using var resp = await _http.GetAsync(uri, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<string> DownloadFileAsync(int modId, string saveDirectory, IProgress<DownloadProgress>? progress, CancellationToken ct = default, string? mode = null)
    {
            var downloadPath = $"mods/{modId}/download" + (string.IsNullOrWhiteSpace(mode) ? "" : "?mode=" + Uri.EscapeDataString(mode));
        using var linkReq = new HttpRequestMessage(HttpMethod.Get, EndpointUrl(downloadPath));
        ApplyAuth(linkReq);
        using var linkResp = await _http.SendAsync(linkReq, ct);
        linkResp.EnsureSuccessStatusCode();
        var linkJson = await linkResp.Content.ReadAsStringAsync(ct);
        var download = JsonSerializer.Deserialize<DownloadLinkResponse>(linkJson, JsonOpts)
            ?? throw new InvalidDataException("The download API returned an empty response.");
        if (string.IsNullOrWhiteSpace(download.Url) ||
            !Uri.TryCreate(download.Url, UriKind.Absolute, out var downloadUri) ||
            (downloadUri.Scheme != Uri.UriSchemeHttps && downloadUri.Scheme != Uri.UriSchemeHttp))
            throw new InvalidDataException("The download API did not return a valid download URL.");

        using var fileReq = new HttpRequestMessage(HttpMethod.Get, downloadUri);
        using var resp = await _http.SendAsync(fileReq, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var totalBytes = resp.Content.Headers.ContentLength ?? -1L;
        var fileName = string.IsNullOrWhiteSpace(download.FileName) ? "download.zip" : download.FileName;
        foreach (var c in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_');
        var filePath = Path.Combine(saveDirectory, fileName);
        await using var contentStream = await resp.Content.ReadAsStreamAsync(ct);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;
        var lastReport = DateTime.UtcNow;
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            if (progress != null && (DateTime.UtcNow - lastReport).TotalMilliseconds > 200)
            {
                lastReport = DateTime.UtcNow;
                progress.Report(new DownloadProgress
                {
                    Percent = totalBytes > 0 ? (float)totalRead / totalBytes * 100f : -1f,
                    BytesReceived = totalRead,
                    TotalBytes = totalBytes,
                    StatusText = FmtBytes(totalRead) + (totalBytes > 0 ? $" / {FmtBytes(totalBytes)}" : "")
                });
            }
        }
        progress?.Report(new DownloadProgress { Percent = 100f, BytesReceived = totalRead, TotalBytes = totalRead, StatusText = FmtBytes(totalRead) });
        return filePath;
    }

    private sealed class DownloadLinkResponse
    {
        [JsonPropertyName("url")] public string Url { get; set; } = "";
        [JsonPropertyName("filename")] public string FileName { get; set; } = "";
    }

    private static string FmtBytes(long b) => b switch { < 1024 => $"{b} B", < 1048576 => $"{b / 1024.0:F1} KB", < 1073741824L => $"{b / 1048576.0:F1} MB", _ => $"{b / 1073741824.0:F2} GB" };
    public void Dispose() => _http.Dispose();
}
