using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ETBTool.Utils
{
    public class UE4SaveEntry
    {
        public string SlotFileName { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    // Read-only GVAS property reader. Display-name metadata is not level progress.
    public sealed class UE4SaveReader
    {
        public List<UE4SaveEntry> Entries { get; } = new();
        public bool IsLoaded { get; private set; }
        public string FilePath { get; private set; } = "";
        public string LastError { get; private set; } = "";

        public bool Load(string path)
        {
            Entries.Clear();
            IsLoaded = false;
            LastError = "";
            FilePath = path;
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length > 64 * 1024 * 1024) throw new InvalidDataException("Save metadata is too large.");
                using var reader = new BinaryReader(stream, Encoding.UTF8);
                if (reader.ReadUInt32() != 0x53415647) throw new InvalidDataException("Not a GVAS save.");
                int version = reader.ReadInt32();
                if (version != 2) throw new InvalidDataException($"Unsupported GVAS version {version}.");
                reader.ReadInt32(); // UE package version
                Skip(reader, 10); // major/minor/patch/changelist
                ReadString(reader, stream.Length);
                int customFormat = reader.ReadInt32();
                if (customFormat != 3) throw new InvalidDataException("Unsupported custom-version format.");
                int customCount = ReadCount(reader, 4096);
                Skip(reader, customCount * 20L);
                ReadString(reader, stream.Length); // SaveGame class
                var entries = new List<UE4SaveEntry>();
                bool found = false;
                for (int property = 0; property < 10000; property++)
                {
                    string name = ReadString(reader, stream.Length);
                    if (name == "None")
                    {
                        if (!found) throw new InvalidDataException("SaveDisplayNamesLookup is absent.");
                        Entries.AddRange(entries);
                        IsLoaded = true;
                        return true;
                    }
                    string type = ReadString(reader, stream.Length);
                    long size = reader.ReadInt64();
                    if (size < 0 || size > stream.Length) throw new InvalidDataException("Invalid property size.");
                    string keyType = "", valueType = "";
                    switch (type)
                    {
                        case "MapProperty":
                            keyType = ReadString(reader, stream.Length);
                            valueType = ReadString(reader, stream.Length);
                            break;
                        case "ArrayProperty":
                        case "SetProperty":
                        case "ByteProperty":
                        case "EnumProperty":
                            ReadString(reader, stream.Length);
                            break;
                        case "StructProperty":
                            ReadString(reader, stream.Length);
                            Skip(reader, 16);
                            break;
                        case "BoolProperty":
                            Skip(reader, 1);
                            break;
                        case "IntProperty": case "Int64Property": case "UInt32Property":
                        case "UInt64Property": case "FloatProperty": case "DoubleProperty":
                        case "StrProperty": case "NameProperty": case "TextProperty":
                        case "ObjectProperty": case "SoftObjectProperty":
                            break;
                        default: throw new InvalidDataException($"Unsupported property type {type}.");
                    }
                    byte hasGuid = reader.ReadByte();
                    if (hasGuid > 1) throw new InvalidDataException("Invalid property GUID marker.");
                    if (hasGuid == 1) Skip(reader, 16);
                    long end = checked(stream.Position + size);
                    if (end > stream.Length) throw new EndOfStreamException();
                    if (name == "SaveDisplayNamesLookup")
                    {
                        if (type != "MapProperty" || keyType != "StrProperty" || valueType != "StrProperty" || found)
                            throw new InvalidDataException("Unsupported display-name map.");
                        int removed = ReadCount(reader, 10000);
                        for (int i = 0; i < removed; i++) ReadString(reader, end);
                        int count = ReadCount(reader, 10000);
                        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < count; i++)
                        {
                            string slot = ReadString(reader, end);
                            string display = ReadString(reader, end);
                            if (!keys.Add(slot)) throw new InvalidDataException("Duplicate save slot.");
                            entries.Add(new UE4SaveEntry { SlotFileName = slot, DisplayName = display });
                        }
                        if (stream.Position != end) throw new InvalidDataException("Invalid display-name map length.");
                        found = true;
                    }
                    stream.Position = end;
                }
                throw new InvalidDataException("Too many save properties.");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Logger.Log($"[UE4SaveReader] {Path.GetFileName(path)}: {ex.Message}");
                return false;
            }
        }

        public UE4SaveEntry? FindBySlotName(string fileName)
        {
            string key = Path.GetFileNameWithoutExtension(fileName);
            return Entries.FirstOrDefault(e => string.Equals(Path.GetFileNameWithoutExtension(e.SlotFileName), key, StringComparison.OrdinalIgnoreCase));
        }

        private static int ReadCount(BinaryReader reader, int max)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > max) throw new InvalidDataException("Invalid collection count.");
            return count;
        }

        private static void Skip(BinaryReader reader, long bytes)
        {
            if (bytes < 0 || bytes > reader.BaseStream.Length - reader.BaseStream.Position) throw new EndOfStreamException();
            reader.BaseStream.Position += bytes;
        }

        private static string ReadString(BinaryReader reader, long end)
        {
            if (end - reader.BaseStream.Position < 4) throw new EndOfStreamException();
            int length = reader.ReadInt32();
            if (length == 0) return "";
            if (length == int.MinValue || Math.Abs(length) > 1024 * 1024) throw new InvalidDataException("Invalid FString length.");
            bool wide = length < 0;
            int bytes = checked(Math.Abs(length) * (wide ? 2 : 1));
            if (bytes > end - reader.BaseStream.Position) throw new EndOfStreamException();
            byte[] data = reader.ReadBytes(bytes);
            int terminator = wide ? 2 : 1;
            if (data.Length != bytes || data[^1] != 0 || (wide && data[^2] != 0)) throw new InvalidDataException("Unterminated FString.");
            var encoding = wide ? (Encoding)new UnicodeEncoding(false, false, true) : new UTF8Encoding(false, true);
            return encoding.GetString(data, 0, bytes - terminator);
        }
    }
}
