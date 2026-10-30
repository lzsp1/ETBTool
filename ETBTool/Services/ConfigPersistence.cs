using System;
using System.IO;
using System.Text.Json;

namespace ETBTool.Services
{
    internal static class ConfigPersistence
    {
        private static readonly object Sync = new();

        public static T Read<T>(string path) where T : new()
        {
            lock (Sync)
            {
                if (!File.Exists(path)) return new T();
                using var stream = File.OpenRead(path);
                return JsonSerializer.Deserialize<T>(stream) ?? throw new JsonException("Invalid configuration.");
            }
        }

        public static void Write<T>(string path, T value)
        {
            lock (Sync)
            {
                var directory = Path.GetDirectoryName(path) ?? throw new IOException("Missing configuration directory.");
                Directory.CreateDirectory(directory);
                var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
                try
                {
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true });
                        stream.Flush(true);
                    }
                    File.Move(temporary, path, true);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
        }
    }
}
