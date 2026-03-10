using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace LezyFileBrowser
{
    // Queries the Everything search engine (voidtools.com) via its SDK DLL for fast
    // directory listings without touching the filesystem. Falls back gracefully when
    // Everything is not installed or not running.
    public static class EverythingSearch
    {
        private const string DLL = "Everything64.dll";

        private const uint EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME = 0x00000004;
        private const uint EVERYTHING_REQUEST_SIZE                    = 0x00000010;
        private const uint EVERYTHING_REQUEST_DATE_MODIFIED           = 0x00000040; // always indexed; DATE_CREATED often is not

        [DllImport(DLL, CharSet = CharSet.Unicode)] private static extern void  Everything_SetSearchW(string s);
        [DllImport(DLL)]                             private static extern void  Everything_SetMatchPath(bool b);
        [DllImport(DLL)]                             private static extern void  Everything_SetRequestFlags(uint f);
        [DllImport(DLL, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]      private static extern bool  Everything_QueryW(bool wait);
        [DllImport(DLL)]                             private static extern uint  Everything_GetNumResults();
        [DllImport(DLL, CharSet = CharSet.Unicode)]  private static extern void  Everything_GetResultFullPathNameW(uint i, StringBuilder sb, uint cap);
        [DllImport(DLL)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Everything_GetResultSize(uint i, out long size);
        [DllImport(DLL)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Everything_GetResultDateModified(uint i, out long ft);
        [DllImport(DLL)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Everything_IsFileResult(uint i);
        [DllImport(DLL)]                             private static extern void  Everything_Reset();

        public static bool IsAvailable { get; private set; }

        // Per-query timing breakdown for profiling (set after each GetFiles call).
        public static long LastQueryMs  { get; private set; }
        public static long LastEnumMs   { get; private set; }

        static EverythingSearch()
        {
            // Provide a custom resolver so we can find Everything64.dll in its default install
            // location even if it is not on PATH or next to the exe.
            NativeLibrary.SetDllImportResolver(typeof(EverythingSearch).Assembly,
                (name, asm, paths) =>
                {
                    if (!name.Equals(DLL, StringComparison.OrdinalIgnoreCase))
                        return IntPtr.Zero;

                    // Already loaded (second call for the same DLL name)?
                    if (NativeLibrary.TryLoad(name, out var h)) return h;

                    // Try Everything's default install directory.
                    var pf = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "Everything", DLL);
                    if (File.Exists(pf) && NativeLibrary.TryLoad(pf, out h)) return h;

                    return IntPtr.Zero;
                });

            try
            {
                Everything_Reset();
                IsAvailable = true;
            }
            catch { IsAvailable = false; }
        }

        // Returns all files under directory (recursive) using Everything's index.
        // Throws if the query fails so the caller can fall back to EnumerateFiles.
        public static FileData[] GetFiles(string directory)
        {
            var searchPath = directory.TrimEnd('\\', '/') + "\\";

            Everything_SetSearchW(searchPath);
            Everything_SetMatchPath(true);
            Everything_SetRequestFlags(
                EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME |
                EVERYTHING_REQUEST_SIZE |
                EVERYTHING_REQUEST_DATE_MODIFIED);

            var sw = Stopwatch.StartNew();
            if (!Everything_QueryW(true))
                throw new InvalidOperationException("Everything_QueryW returned false");
            LastQueryMs = sw.ElapsedMilliseconds;
            sw.Restart();

            uint count = Everything_GetNumResults();
            var result = new List<FileData>((int)count);
            var sb = new StringBuilder(4096);

            for (uint i = 0; i < count; i++)
            {
                if (!Everything_IsFileResult(i)) continue;

                sb.Clear();
                Everything_GetResultFullPathNameW(i, sb, (uint)sb.Capacity);
                var fullPath = sb.ToString();

                // Client-side guard: Everything's path: filter is a substring match.
                if (!fullPath.StartsWith(searchPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                Everything_GetResultSize(i, out long size);

                DateTime date = DateTime.MinValue;
                if (Everything_GetResultDateModified(i, out long ft) && ft > 0)
                    date = DateTime.FromFileTime(ft);

                result.Add(new FileData(fullPath, size, date));
            }

            LastEnumMs = sw.ElapsedMilliseconds;
            return result.ToArray();
        }
    }
}
