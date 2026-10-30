using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Services
{
    public sealed class ModTranslationService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly CancellationTokenSource _cancel = new();
        private Dictionary<string, string> _remote = new(StringComparer.OrdinalIgnoreCase);
        private int _generation;
        private bool _disposed;
        public string StatusMessage { get; private set; } = "网站翻译：尚未连接";
        public bool IsConnected { get; private set; }
        public event Action? TranslationsChanged;

        public ModTranslationService(HttpMessageHandler? handler = null)
        {
            _http = handler == null ? new HttpClient() : new HttpClient(handler, false);
            _http.Timeout = TimeSpan.FromSeconds(15);
            _http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        }

        public static string NormalizeFileName(string name)
        {
            name = Path.GetFileName(name.Trim());
            return name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) ? name[..^9] : name;
        }

        public string? GetTranslation(string name) =>
            _remote.TryGetValue(NormalizeFileName(name), out var translated) ? translated : null;
        public bool HasTranslation(string name) => GetTranslation(name) != null;

        public async Task LoadAsync()
        {
            if (_disposed) return;
            int generation = Interlocked.Increment(ref _generation);
            _remote = new(StringComparer.OrdinalIgnoreCase);
            IsConnected = false;
            StatusMessage = "网站翻译：正在连接";
            TranslationsChanged?.Invoke();
            try
            {
                using var response = await _http.GetAsync(GamePaths.ModNamesUrl, _cancel.Token);
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(_cancel.Token));
                if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty("items", out var items))
                    throw new InvalidDataException("网站翻译响应缺少 items 字段。");
                var next = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (items.ValueKind == JsonValueKind.Object)
                {
                    foreach (var entry in items.EnumerateObject())
                    {
                        if (entry.Value.ValueKind != JsonValueKind.String) throw new InvalidDataException("网站翻译内容格式无效。");
                        var value = entry.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(entry.Name) && !string.IsNullOrWhiteSpace(value))
                            next[NormalizeFileName(entry.Name)] = value;
                    }
                }
                else if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() != 0)
                    throw new InvalidDataException("网站翻译列表格式无效。");
                if (_disposed || generation != _generation) return;
                _remote = next;
                IsConnected = true;
                StatusMessage = $"网站翻译：已连接，共 {next.Count} 条";
                Logger.Log($"网站翻译已刷新，共 {next.Count} 条");
            }
            catch (Exception ex)
            {
                if (_disposed || generation != _generation) return;
                _remote = new(StringComparer.OrdinalIgnoreCase);
                StatusMessage = "网站翻译：连接失败，显示原文件名";
                Logger.Log($"在线翻译请求失败: {ex.Message}");
            }
            if (!_disposed && generation == _generation) TranslationsChanged?.Invoke();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Interlocked.Increment(ref _generation);
            _cancel.Cancel();
            _http.Dispose();
            _cancel.Dispose();
        }
    }
}
