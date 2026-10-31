using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ETBTool.Models;

public class LoginResponse { [JsonPropertyName("token")] public string Token { get; set; } = ""; [JsonPropertyName("user")] public UserDto User { get; set; } = new(); [JsonPropertyName("error")] public string? Error { get; set; } [JsonPropertyName("banned")] public bool Banned { get; set; } [JsonPropertyName("ban_reason")] public string? BanReason { get; set; } }
public class UserDto { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("username")] public string Username { get; set; } = ""; [JsonPropertyName("email")] public string Email { get; set; } = ""; }
public class MessageResponse { [JsonPropertyName("message")] public string? Message { get; set; } [JsonPropertyName("error")] public string? Error { get; set; } }
public class StatsResponse { [JsonPropertyName("users")] public int Users { get; set; } [JsonPropertyName("mods")] public int Mods { get; set; } [JsonPropertyName("tools")] public int Tools { get; set; } }
public class ModListResponse { [JsonPropertyName("mods")] public List<ModItem> Mods { get; set; } = new(); [JsonPropertyName("page")] public int Page { get; set; } [JsonPropertyName("pages")] public int Pages { get; set; } }

public class ModItem
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("game_name")] public string GameName { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("uploader_name")] public string UploaderName { get; set; } = "";
    [JsonPropertyName("author")] public string Author { get; set; } = "";
    [JsonPropertyName("language")] public string Language { get; set; } = "";
    [JsonPropertyName("instructions")] public string Instructions { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("cover_image")] public string? CoverImage { get; set; }
    [JsonPropertyName("downloads")] public int Downloads { get; set; }
    [JsonPropertyName("images")] public List<ModImage> Images { get; set; } = new();
    [JsonPropertyName("storage_type")] public string StorageType { get; set; } = "site";
    [JsonPropertyName("external_url")] public string ExternalUrl { get; set; } = "";
    [JsonPropertyName("dependencies")] public List<ModDependency> Dependencies { get; set; } = new();
    [JsonPropertyName("version")] public JsonElement VersionRaw { get; set; }
    [JsonIgnore] public string Version { get { try { return VersionRaw.ValueKind switch { JsonValueKind.String => VersionRaw.GetString() ?? "", JsonValueKind.Number => VersionRaw.GetDouble().ToString("0.##"), JsonValueKind.Null => "", JsonValueKind.Undefined => "", _ => VersionRaw.ToString() }; } catch { return ""; } } }
    [JsonPropertyName("is_disabled")] public JsonElement IsDisabledRaw { get; set; }
    [JsonIgnore] public bool IsDisabled { get { try { return IsDisabledRaw.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => IsDisabledRaw.GetInt32() != 0, JsonValueKind.String => (IsDisabledRaw.GetString() ?? "") == "1" || (IsDisabledRaw.GetString() ?? "").ToLower() == "true", _ => false }; } catch { return false; } } }
    [JsonPropertyName("is_pinned")] public JsonElement IsPinnedRaw { get; set; }
    [JsonIgnore] public bool IsPinned { get { try { return IsPinnedRaw.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => IsPinnedRaw.GetInt32() != 0, JsonValueKind.String => (IsPinnedRaw.GetString() ?? "") == "1" || (IsPinnedRaw.GetString() ?? "").ToLower() == "true", _ => false }; } catch { return false; } } }
}

public class ModImage { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("image_path")] public string? ImagePath { get; set; } [JsonPropertyName("path")] public string? Path { get; set; } public string FileName => (ImagePath ?? Path ?? "").Split('/').LastOrDefault() ?? ""; public string? FullUrl => string.IsNullOrEmpty(FileName) ? null : $"/uploads/images/{FileName}"; }
public class ModDependency { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("mod_id")] public int ModId { get; set; } [JsonPropertyName("name")] public string Name { get; set; } = ""; [JsonPropertyName("mod_name")] public string? ModName { get; set; } public string DisplayName => !string.IsNullOrEmpty(Name) ? Name : !string.IsNullOrEmpty(ModName) ? ModName : $"#{Id}"; }
public class DownloadProgress { public float Percent { get; set; } public long BytesReceived { get; set; } public long TotalBytes { get; set; } public string StatusText { get; set; } = ""; }
public class TokenPayload { [JsonPropertyName("id")] public int Id { get; set; } [JsonPropertyName("username")] public string Username { get; set; } = ""; [JsonPropertyName("email")] public string Email { get; set; } = ""; [JsonPropertyName("exp")] public long Exp { get; set; } }