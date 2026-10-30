using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ETBTool.Services
{
    public static class ModFileOperations
    {
        private static readonly string[] PackageExtensions = { ".pak", ".utoc", ".ucas" };
        public static string DisplayFileName(string path) => ModTranslationService.NormalizeFileName(Path.GetFileName(path));
        public static bool IsDisabled(string path) => path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
        private static bool IsPackage(string path) => PackageExtensions.Contains(Path.GetExtension(DisplayFileName(path)), StringComparer.OrdinalIgnoreCase);
        private static bool SamePath(string first, string second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        private static bool Inside(string path, string directory) => path.StartsWith(directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        private static void CheckPath(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path)) throw new IOException($"所选项目已不存在：{Path.GetFileName(path)}");
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("不支持对链接文件或链接目录执行模组操作。");
        }

        private static List<string> Selection(IEnumerable<string> selected)
        {
            var paths = selected.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (paths.Count == 0) throw new InvalidOperationException("请至少选择一个模组。");
            foreach (var path in paths) CheckPath(path);
            return paths.Where(path => !paths.Any(parent => !SamePath(path, parent) && Directory.Exists(parent) && Inside(path, parent))).ToList();
        }

        private static IEnumerable<string> FilesIn(string directory)
        {
            var pending = new Stack<string>();
            pending.Push(directory);
            while (pending.Count > 0)
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(pending.Pop()))
                {
                    CheckPath(entry);
                    if (Directory.Exists(entry)) pending.Push(entry); else yield return entry;
                }
            }
        }

        private static IEnumerable<string> Companions(string path)
        {
            yield return path;
            if (!IsPackage(path)) yield break;
            var baseName = Path.GetFileNameWithoutExtension(DisplayFileName(path));
            foreach (var sibling in Directory.EnumerateFiles(Path.GetDirectoryName(path)!))
                if (!SamePath(sibling, path) && IsPackage(sibling) &&
                    string.Equals(Path.GetFileNameWithoutExtension(DisplayFileName(sibling)), baseName, StringComparison.OrdinalIgnoreCase))
                {
                    CheckPath(sibling);
                    yield return sibling;
                }
        }

        public static void Pack(IEnumerable<string> selected, string destination)
        {
            var sources = Selection(selected);
            destination = Path.GetFullPath(destination);
            if (sources.Any(path => SamePath(path, destination) || (Directory.Exists(path) && Inside(destination, path))))
                throw new IOException("压缩包不能覆盖所选文件，也不能保存到所选文件夹内部。");
            var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name, string path)
            {
                name = string.Join("/", name.Replace('\\', '/').Split('/').Select(DisplayFileName));
                if (entries.TryGetValue(name, out var existing))
                {
                    if (SamePath(existing, path)) return;
                    throw new IOException($"打包后出现重复文件名：{name}。请先处理同名的启用与禁用文件。");
                }
                entries.Add(name, path);
            }
            foreach (var source in sources)
            {
                if (Directory.Exists(source))
                    foreach (var path in FilesIn(source)) Add(DisplayFileName(source) + "/" + Path.GetRelativePath(source, path), path);
                else
                    foreach (var path in Companions(source)) Add(DisplayFileName(path), path);
            }
            if (entries.Values.Any(path => SamePath(path, destination))) throw new IOException("压缩包不能覆盖所选模组的配套资源文件。");
            if (entries.Count == 0) throw new IOException("所选文件夹没有可打包的文件。");
            var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                    foreach (var entry in entries) zip.CreateEntryFromFile(entry.Value, entry.Key, CompressionLevel.Optimal);
                File.Move(temp, destination, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }

        public static IReadOnlyDictionary<string, string> Toggle(IEnumerable<string> selected, bool enable)
        {
            var sources = Selection(selected);
            var moves = new List<(string Source, string Target, bool Directory)>();
            var selectedAfter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string Destination(string path) => enable ? (IsDisabled(path) ? path[..^9] : path) : (IsDisabled(path) ? path : path + ".disabled");
            void AddFile(string path)
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(DisplayFileName(path)), "EscapeTheBackrooms-WindowsNoEditor", StringComparison.OrdinalIgnoreCase))
                    throw new IOException("不能更改游戏本体资源的启用状态。");
                var target = Destination(path);
                if (!SamePath(path, target) && !moves.Any(move => SamePath(move.Source, path))) moves.Add((path, target, false));
            }
            foreach (var source in sources)
            {
                if (Directory.Exists(source))
                {
                    var files = FilesIn(source).Where(IsPackage).ToList();
                    if (files.Count == 0) throw new IOException($"文件夹 {DisplayFileName(source)} 不含 PAK 模组资源，请进入后选择具体文件操作。");
                    foreach (var file in files) AddFile(file);
                    var target = Destination(source);
                    if (!SamePath(source, target)) moves.Add((source, target, true));
                }
                else foreach (var file in Companions(source)) AddFile(file);
                selectedAfter[source] = Destination(source);
            }
            foreach (var move in moves)
                if (File.Exists(move.Target) || Directory.Exists(move.Target))
                    throw new IOException($"目标名称已存在，未更改任何文件：{Path.GetFileName(move.Target)}");
            var completed = new List<(string Source, string Target, bool Directory)>();
            try
            {
                foreach (var move in moves)
                {
                    if (move.Directory) Directory.Move(move.Source, move.Target); else File.Move(move.Source, move.Target);
                    completed.Add(move);
                }
            }
            catch (Exception failure)
            {
                var rollback = new List<Exception>();
                foreach (var move in Enumerable.Reverse(completed))
                    try { if (move.Directory) Directory.Move(move.Target, move.Source); else File.Move(move.Target, move.Source); }
                    catch (Exception error) { rollback.Add(error); }
                if (rollback.Count > 0) throw new AggregateException("切换状态失败，部分文件无法恢复，请查看日志并检查实际文件名。", new[] { failure }.Concat(rollback));
                throw new IOException("切换状态失败，已恢复先前的文件名。请检查文件占用或目录权限。", failure);
            }
            return selectedAfter;
        }
    }
}
