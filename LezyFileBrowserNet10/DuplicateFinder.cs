using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LezyFileBrowser
{
    public static class DuplicateFinder
    {
        // Returns a map of each duplicate file path -> list of its content-identical peers in the list.
        // Only files matching videoExts and larger than minSizeBytes are considered.
        // Groups by size first so byte reads only happen within same-size candidates.
        public static Dictionary<string, List<FileData>> FindDuplicateGroupsInList(
            IEnumerable<FileData> files,
            string[] videoExts,
            long minSizeBytes,
            int headMb,
            int tailMb,
            int bufSize)
        {
            var result = new Dictionary<string, List<FileData>>(StringComparer.OrdinalIgnoreCase);

            var candidates = files
                .Where(fi => fi.Length > minSizeBytes &&
                             videoExts.Contains(Path.GetExtension(fi.Name).ToLower()))
                .ToList();

            foreach (var group in candidates.GroupBy(fi => fi.Length).Where(g => g.Count() > 1))
            {
                var groupList = group.ToList();
                for (int i = 0; i < groupList.Count; i++)
                    for (int j = i + 1; j < groupList.Count; j++)
                        if (ContentMatches(groupList[i], groupList[j], headMb, tailMb, bufSize))
                        {
                            if (!result.ContainsKey(groupList[i].FullName)) result[groupList[i].FullName] = new List<FileData>();
                            if (!result.ContainsKey(groupList[j].FullName)) result[groupList[j].FullName] = new List<FileData>();
                            result[groupList[i].FullName].Add(groupList[j]);
                            result[groupList[j].FullName].Add(groupList[i]);
                        }
            }

            return result;
        }

        // Returns all files in searchDir (recursive) that are content-identical to target.
        // Only considers video files (by extension) larger than minSizeBytes.
        // Identity = same file size + matching first headMb + last tailMb of content.
        public static List<FileData> FindDuplicates(
            FileData target,
            string searchDir,
            string[] videoExts,
            long minSizeBytes,
            int headMb,
            int tailMb,
            int bufSize)
        {
            var ext = Path.GetExtension(target.Name).ToLower();
            if (!videoExts.Contains(ext)) return new List<FileData>();
            if (target.Length <= minSizeBytes) return new List<FileData>();

            var candidates = Directory
                .GetFiles(searchDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !string.Equals(f, target.FullName, StringComparison.OrdinalIgnoreCase))
                .Select(f =>
                {
                    try { return new FileData(new FileInfo(f)); }
                    catch { return null; }
                })
                .Where(fd =>
                    fd != null &&
                    fd.Length == target.Length &&
                    fd.Length > minSizeBytes &&
                    videoExts.Contains(Path.GetExtension(fd.Name).ToLower()))
                .ToList();

            if (candidates.Count == 0) return new List<FileData>();

            return candidates
                .Where(fd => ContentMatches(target, fd, headMb, tailMb, bufSize))
                .ToList();
        }

        // Searches an already-fetched file list instead of hitting the filesystem.
        // Use this when a cached FileData[] is available to avoid a full directory scan.
        public static List<FileData> FindDuplicatesInList(
            FileData target,
            FileData[] allFiles,
            string[] videoExts,
            long minSizeBytes,
            int headMb,
            int tailMb,
            int bufSize)
        {
            var ext = Path.GetExtension(target.Name).ToLower();
            if (!videoExts.Contains(ext)) return new List<FileData>();
            if (target.Length <= minSizeBytes) return new List<FileData>();

            var candidates = allFiles
                .Where(fd => !string.Equals(fd.FullName, target.FullName, StringComparison.OrdinalIgnoreCase)
                          && fd.Length == target.Length
                          && fd.Length > minSizeBytes
                          && videoExts.Contains(Path.GetExtension(fd.Name).ToLower()))
                .ToList();

            if (candidates.Count == 0) return new List<FileData>();

            return candidates
                .Where(fd => ContentMatches(target, fd, headMb, tailMb, bufSize))
                .ToList();
        }

        private static bool ContentMatches(FileData a, FileData b, int headMb, int tailMb, int bufSize)
        {
            long headBytes = (long)headMb * 1024 * 1024;
            long tailBytes = (long)tailMb * 1024 * 1024;

            if (!RegionMatches(a.FullName, b.FullName, 0, Math.Min(headBytes, a.Length), bufSize))
                return false;

            long tailOffset = a.Length - tailBytes;
            if (tailOffset > headBytes)
            {
                if (!RegionMatches(a.FullName, b.FullName, tailOffset, Math.Min(tailBytes, a.Length - tailOffset), bufSize))
                    return false;
            }

            return true;
        }

        private static bool RegionMatches(string pathA, string pathB, long offset, long length, int bufSize)
        {
            if (length <= 0) return true;

            var bufA = new byte[bufSize];
            var bufB = new byte[bufSize];

            try
            {
                using var fsA = new FileStream(pathA, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var fsB = new FileStream(pathB, FileMode.Open, FileAccess.Read, FileShare.Read);
                fsA.Seek(offset, SeekOrigin.Begin);
                fsB.Seek(offset, SeekOrigin.Begin);

                long remaining = length;
                while (remaining > 0)
                {
                    int toRead = (int)Math.Min(remaining, bufSize);
                    int ra = fsA.Read(bufA, 0, toRead);
                    int rb = fsB.Read(bufB, 0, toRead);
                    if (ra != rb || ra == 0) return false;
                    for (int i = 0; i < ra; i++)
                        if (bufA[i] != bufB[i]) return false;
                    remaining -= ra;
                }
            }
            catch { return false; }

            return true;
        }

        // Returns a map of fullPath -> list of similar fullPaths for entries whose display names
        // share at least minCommonTokens leading dot-separated tokens (after stripping trailing tags).
        public static Dictionary<string, List<string>> FindSimilarNameGroups(
            IEnumerable<(string fullPath, string displayName)> entries,
            int minCommonTokens)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var list = entries.Select(e => (e.fullPath, tokens: TokenizeName(e.displayName))).ToList();

            // Group by first token before the O(n²) comparison: two names can only share
            // minCommonTokens leading tokens if they share the very first token.
            var byFirstToken = list
                .Where(e => e.tokens.Length > 0)
                .GroupBy(e => e.tokens[0], StringComparer.OrdinalIgnoreCase);

            foreach (var group in byFirstToken)
            {
                var g = group.ToList();
                for (int i = 0; i < g.Count; i++)
                    for (int j = i + 1; j < g.Count; j++)
                        if (CommonPrefixCount(g[i].tokens, g[j].tokens) >= minCommonTokens)
                        {
                            if (!result.ContainsKey(g[i].fullPath)) result[g[i].fullPath] = new List<string>();
                            if (!result.ContainsKey(g[j].fullPath)) result[g[j].fullPath] = new List<string>();
                            result[g[i].fullPath].Add(g[j].fullPath);
                            result[g[j].fullPath].Add(g[i].fullPath);
                        }
            }

            return result;
        }

        // Strips trailing [...] and (...) tags, removes extension, splits by '.'.
        private static readonly Regex _trailingTag = new Regex(@"[\[\(][^\]\)]*[\]\)]\s*$", RegexOptions.Compiled);
        private static string[] TokenizeName(string name)
        {
            var n = _trailingTag.Replace(name, "").TrimEnd('.', ' ', '-', '_');
            n = Path.GetFileNameWithoutExtension(n);
            return n.Split('.', StringSplitOptions.RemoveEmptyEntries);
        }

        private static int CommonPrefixCount(string[] a, string[] b)
        {
            int n = Math.Min(a.Length, b.Length);
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                if (!a[i].Equals(b[i], StringComparison.OrdinalIgnoreCase)) break;
                count++;
            }
            return count;
        }
    }
}
