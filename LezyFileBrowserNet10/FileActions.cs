using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LezyFileBrowser
{
    public enum WorkOnFileOrDir { File, Dir };
    public class FileActions
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        // Called with a pre-fetched (cached) list of files in fi's directory.
        // Avoids a Directory.GetFiles call per file — callers should cache per-directory.
        public WorkOnFileOrDir ShouldWorkOnFileOrDir(FileData fi, FileData[] cachedDirFiles)
        {
            string fileNameWOExt = Path.GetFileNameWithoutExtension(fi.Name).ToLower();
            string parentDirName = fi.Directory.Name.ToLower();

            if (parentDirName.Length <= 3)
                return WorkOnFileOrDir.File;

            if (cachedDirFiles.Length > MainForm.MAX_FILES_IN_DIR_TO_DELETE)
                return WorkOnFileOrDir.File;

            if (parentDirName.Equals(fileNameWOExt) || parentDirName.IndexOf(fileNameWOExt) == 0)
                return WorkOnFileOrDir.Dir;

            int countLarge = 0;
            for (int i = 0; i < cachedDirFiles.Length; i++)
                if (cachedDirFiles[i].Length > MainForm.MIN_FILE_SIZE_BYTES)
                    countLarge++;

            if (countLarge == 1)
                return WorkOnFileOrDir.Dir;

            return WorkOnFileOrDir.File;
        }

        public WorkOnFileOrDir ShouldWorkOnFileOrDir(FileData fi)
        {
            var filesInDir = fi.Directory.GetFiles("*.*", SearchOption.AllDirectories)
                .Where(f => f.Length > MainForm.MIN_DIR_SIZE_FOR_COUNT_BYTES)
                .Select(f => new FileData(f)).ToArray();
            return ShouldWorkOnFileOrDir(fi, filesInDir);
        }

        public void LaunchFile(FileData fi, bool useAltPlayer, bool fullScreen, bool onTop, bool keepFocus, IntPtr ownerHandle)
        {
            var videoExtensions = ConfigurationManager.AppSettings["VIDEO_EXTENSIONS"]
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var ext = Path.GetExtension(fi.Name).ToLower();
            if (!videoExtensions.Contains(ext)) return;

            string targetPath = ResolvePlayerPath(fi.FullName);

            string prefix = useAltPlayer ? "PLAYER_2" : "PLAYER_1";
            string exe      = ConfigurationManager.AppSettings[$"{prefix}_EXE"];
            string fsArg    = fullScreen ? " " + ConfigurationManager.AppSettings[$"{prefix}_FS_ARG"]     : "";
            string topArg   = onTop      ? " " + ConfigurationManager.AppSettings[$"{prefix}_ON_TOP_ARG"] : "";
            string winStyle = ConfigurationManager.AppSettings[$"{prefix}_WINDOW_STYLE"] ?? "Normal";
            var windowStyle = Enum.TryParse<ProcessWindowStyle>(winStyle, true, out var ws) ? ws : ProcessWindowStyle.Normal;

            string args;
            if (useAltPlayer)
                args = $"{fsArg}{topArg} \"{targetPath}\"".TrimStart();
            else
            {
                string baseArgs = ConfigurationManager.AppSettings["PLAYER_1_ARGS"];
                args = $"{baseArgs}{fsArg}{topArg} \"{targetPath}\"";
            }

            KillPlayerProcesses();

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WindowStyle = windowStyle,
                UseShellExecute = false,
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                try { proc.PriorityClass = ProcessPriorityClass.High; } catch { }
            }

            if (keepFocus)
                Task.Run(() => FocusPlayerWindow(ownerHandle));
        }

        private void KillPlayerProcesses()
        {
            string[] names = ConfigurationManager.AppSettings["PLAYER_PROCESS_NAMES"]
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var name in names)
            {
                try
                {
                    foreach (var p in Process.GetProcessesByName(name.Trim()))
                        p.Kill();
                }
                catch { }
            }
        }

        // Creates a symlink in %TEMP% when the path exceeds Windows MAX_PATH (260).
        private string ResolvePlayerPath(string fullPath)
        {
            int threshold = int.Parse(ConfigurationManager.AppSettings["LONG_PATH_THRESHOLD"]);
            if (fullPath.Length <= threshold) return fullPath;

            string linkPath = Path.Combine(Path.GetTempPath(), "tmp.mp4");
            try { File.Delete(linkPath); } catch { }
            CreateSymbolicLink(linkPath, fullPath, 0);
            return linkPath;
        }

        private void FocusPlayerWindow(IntPtr ownerHandle)
        {
            int pollMs = int.Parse(ConfigurationManager.AppSettings["PLAYER_FOCUS_POLL_MS"]);
            string[] playerProcessNames = ConfigurationManager.AppSettings["PLAYER_PROCESS_NAMES"]
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // Phase 1: wait indefinitely until a player process with a window appears
            Process player = null;
            while (player == null)
            {
                Thread.Sleep(pollMs);
                try
                {
                    foreach (var name in playerProcessNames)
                    {
                        var procs = Process.GetProcessesByName(name.Trim());
                        foreach (var p in procs)
                        {
                            if (p.MainWindowHandle != IntPtr.Zero)
                            {
                                player = p;
                                break;
                            }
                        }
                        if (player != null) break;
                    }
                }
                catch { }
            }

            // Phase 2: keep enforcing focus until player exits
            while (true)
            {
                Thread.Sleep(pollMs);
                try
                {
                    player.Refresh();
                    if (player.HasExited) break;

                    var hwnd = player.MainWindowHandle;
                    if (hwnd != IntPtr.Zero && GetForegroundWindow() != hwnd)
                        SetForegroundWindow(hwnd);
                }
                catch { break; }
            }

            // Player exited — return focus to our window
            if (ownerHandle != IntPtr.Zero)
                SetForegroundWindow(ownerHandle);
        }
    }
}
