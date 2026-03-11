using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LezyFileBrowser
{
    public partial class MainForm : Form
    {
        private LocalCache localCache = new LocalCache();

        private string inputDir;
        private string okDir;
        private int sortDirection;
        private bool inPlaceBrowsing;
        private bool noCache;
        private bool noDupeCheck;
        private bool profileLog;
        private bool moveWithRobocopy;
        private int MAX_LIST_ITEMS;
        private long MIN_FILE_SIZE_MB;

        private string _activeProfileName;
        private MenuStrip           _menuStrip;
        private ToolStripMenuItem   _menuItemProfiles;

        private static Color ParseColor(string value)
        {
            var p = value.Split(',');
            return Color.FromArgb(int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
        }

        public static long MIN_FILE_SIZE_BYTES = long.MinValue;
        public static int MAX_FILES_IN_DIR_TO_DELETE;
        public static long MIN_DIR_SIZE_FOR_COUNT_BYTES;

        private int ioRetryDelayMs;
        private int dupeCheckHeadMb;
        private int dupeCheckTailMb;
        private int similarNameMinTokens;
        private string[] videoExtensions;

        private string scriptSabnzbd;
        private string browserExe;
        private string btSearchUrl1;
        private string btSearchUrl2;
        private string relSuffix;
        private int pendingDeletePollMs;
        private int fileOpQueuePollMs;
        private int dupeCheckBufSize;
        private Color colorAlreadySaved;
        private Color colorDupe;
        private Color colorSimilarName;

        private List<string> sbLogs = new List<string>();

        private FileOperations fileOperations;
        private FileActions fileActions = new FileActions();
        private Util util = new Util();

        private bool loaded = false;
        private int lastCount = 0;
        private long lastTotalSize = 0;

        private Dictionary<string, List<FileData>> _dupeGroups = new Dictionary<string, List<FileData>>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _alreadyInOkPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, List<string>> _similarNameGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private bool _isRefreshing = false;

        // Cached groups from the last full computation; reused while the displayed set only shrinks.
        private HashSet<string> _groupsCachedForPaths;
        private Dictionary<string, List<FileData>> _cachedDupeGroups;
        private Dictionary<string, List<string>> _cachedSimilarGroups;

        private List<PendingDelete> pendingDeletes = new List<PendingDelete>();
        private System.Windows.Forms.Timer timPendingDelete;

        public MainForm()
        {
            InitializeComponent();
            EnableDoubleBuffer(lstFiles);
            BuildMenuStrip();
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (loaded && lstFiles.Items.Count == 0) { RefreshItemsListing(); }
            lstFiles.Focus();
        }

        private void AdjustColumnWidths()
        {
            const int fixedColumnsWidth = 140 + 120 + 40; // colDate + colSize + colWod
            int nameWidth = lstFiles.ClientSize.Width - fixedColumnsWidth - SystemInformation.VerticalScrollBarWidth;
            colDirName.Width = Math.Max(nameWidth, 100);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            AdjustColumnWidths();
        }

        private void lstFiles_Resize(object sender, EventArgs e)
        {
            AdjustColumnWidths();
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            AdjustColumnWidths();
        }

        private static void EnableDoubleBuffer(ListView lv)
        {
            typeof(ListView)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(lv, true);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // ── Global settings (App.config) ────────────────────────────────
            MAX_FILES_IN_DIR_TO_DELETE   = int.Parse(ConfigurationManager.AppSettings["MAX_FILES_IN_DIR_TO_DELETE"]);
            MIN_DIR_SIZE_FOR_COUNT_BYTES = long.Parse(ConfigurationManager.AppSettings["MIN_DIR_SIZE_FOR_COUNT_KB"]) * 1024;
            ioRetryDelayMs       = int.Parse(ConfigurationManager.AppSettings["IO_RETRY_DELAY_MS"]);
            dupeCheckHeadMb      = int.Parse(ConfigurationManager.AppSettings["DUPE_CHECK_HEAD_MB"]);
            dupeCheckTailMb      = int.Parse(ConfigurationManager.AppSettings["DUPE_CHECK_TAIL_MB"]);
            similarNameMinTokens = int.Parse(ConfigurationManager.AppSettings["SIMILAR_NAME_MIN_TOKENS"]);
            videoExtensions      = ConfigurationManager.AppSettings["VIDEO_EXTENSIONS"]
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            scriptSabnzbd        = ConfigurationManager.AppSettings["SCRIPT_SABNZBD"];
            browserExe           = ConfigurationManager.AppSettings["BROWSER_EXE"];
            btSearchUrl1         = ConfigurationManager.AppSettings["BT_SEARCH_URL_1"];
            btSearchUrl2         = ConfigurationManager.AppSettings["BT_SEARCH_URL_2"];
            relSuffix            = ConfigurationManager.AppSettings["REL_SUFFIX"];
            pendingDeletePollMs  = int.Parse(ConfigurationManager.AppSettings["PENDING_DELETE_POLL_MS"]);
            fileOpQueuePollMs    = int.Parse(ConfigurationManager.AppSettings["FILEOP_QUEUE_POLL_MS"]);
            dupeCheckBufSize     = int.Parse(ConfigurationManager.AppSettings["DUPE_CHECK_BUFFER_SIZE"]);
            colorAlreadySaved    = ParseColor(ConfigurationManager.AppSettings["COLOR_ALREADY_SAVED"]);
            colorDupe            = ParseColor(ConfigurationManager.AppSettings["COLOR_DUPE"]);
            colorSimilarName     = ParseColor(ConfigurationManager.AppSettings["COLOR_SIMILAR_NAME"]);

            // ── Profile loading ──────────────────────────────────────────────
            var args = Environment.GetCommandLineArgs();
            string requestedProfileName = args.Length > 1 ? args[1] : ProfileRegistry.GetLastProfile();
            Profile profile = requestedProfileName != null ? ProfileRegistry.Load(requestedProfileName) : null;

            if (profile == null)
            {
                // First run or unknown profile — show manager in startup mode
                using var dlg = new ProfileManagerForm(currentProfileName: null, startupMode: true);
                if (dlg.ShowDialog(this) != System.Windows.Forms.DialogResult.OK || dlg.SelectedProfile == null)
                {
                    Environment.Exit(0);
                    return;
                }
                profile = dlg.SelectedProfile;
            }

            ApplyProfile(profile);
            ProfileRegistry.SetLastProfile(profile.Name);
            Text = $"LezyFileBrowser — {_activeProfileName}";
            RefreshProfilesMenu();

            MIN_FILE_SIZE_BYTES = MIN_FILE_SIZE_MB * 1024 * 1024;

            // robocopyOverride for mount-point detection block below
            var robocopyOverride = string.IsNullOrEmpty(profile.MoveWithRobocopy) ? null : profile.MoveWithRobocopy;

            LogMessage(
                "\r\n\r\n" +
                "      Enter       - Launch (player 1)" + "\r\n" +
                "         F9       - Launch (player 2 / alt)" + "\r\n" +
                "         F3       - Open in Total Commander" + "\r\n" +
                "         F4       - SIR Item" + "\r\n" +
                "  LShift+F4       - SIR Item (no suffix)" + "\r\n" +
                "         F5       - Refresh" + "\r\n" +
                "  LShift+F5       - Refresh (force read from disk)" + "\r\n" +
                "         F6       - BT Search" + "\r\n" +
                "         F7       - Sort Toggle (date / name / random)" + "\r\n" +
                "        F11       - Save for later (txt)" + "\r\n" +
                "      Space       - Save Item" + "\r\n" +
                "  DEL / \\ / BS   - Delete Item" + "\r\n" +
                "LShift+DEL / \\   - Delete Parent" + "\r\n" +
                "     Ctrl+Z       - Undo last delete" + "\r\n" +
                "\r\n\r\n"
                );


            if (inPlaceBrowsing)
                okDir = inputDir;

            LoadCheckboxState();

            ulong inputFree, inputTotal, okFree, okTotal;
            Util.DriveFreeBytes(inputDir, out inputFree, out inputTotal);
            Util.DriveFreeBytes(okDir, out okFree, out okTotal);

            // Detect volume mount points (handles NTFS mount points, not just drive letters)
            var inputMount = Util.GetVolumeMountPoint(inputDir);
            var okMount    = Util.GetVolumeMountPoint(okDir);
            bool sameVolume = string.Equals(inputMount, okMount, StringComparison.OrdinalIgnoreCase);

            string robocopyReason;
            if (robocopyOverride != null)
            {
                robocopyReason = $"X_MOVE_WITH_ROBOCOPY override = {robocopyOverride}";
            }
            else
            {
                moveWithRobocopy = !sameVolume;
                robocopyReason = sameVolume
                    ? "auto: same volume → Directory.Move"
                    : "auto: different volumes → Robocopy";
            }

            var sortLabel = SortLabel();

            LogMessage(
                "\r\n" +
                "  --- Directories -------------------------------------------------------\r\n" +
                $"  Input dir    [ {inputDir} ]\r\n" +
                $"               mount point  [ {inputMount} ]\r\n" +
                $"               free {inputFree / 1024 / 1024 / 1024} GB / total {inputTotal / 1024 / 1024 / 1024} GB\r\n" +
                $"  OK dir       [ {okDir} ]\r\n" +
                $"               mount point  [ {okMount} ]\r\n" +
                $"               free {okFree / 1024 / 1024 / 1024} GB / total {okTotal / 1024 / 1024 / 1024} GB\r\n" +
                $"               same volume  {sameVolume}\r\n" +
                "\r\n" +
                "  --- Configuration -----------------------------------------------------\r\n" +
                $"  Profile          {_activeProfileName}\r\n" +
                $"  Sort             {sortLabel}\r\n" +
                $"  Min file size    {MIN_FILE_SIZE_MB} MB\r\n" +
                $"  Max list items   {(MAX_LIST_ITEMS == int.MaxValue ? "unlimited" : MAX_LIST_ITEMS.ToString())}\r\n" +
                $"  In-place browse  {inPlaceBrowsing}\r\n" +
                $"  No cache         {noCache}\r\n" +
                $"  No dupe check    {noDupeCheck}\r\n" +
                $"  Robocopy move    {moveWithRobocopy}  ({robocopyReason})\r\n" +
                "  -----------------------------------------------------------------------\r\n"
            );

            var sep = Path.DirectorySeparatorChar.ToString();
            if (Directory.Exists(inputDir) && !inputDir.EndsWith(sep)) inputDir += sep;
            if (Directory.Exists(okDir)    && !okDir.EndsWith(sep))    okDir    += sep;

            if (!Directory.Exists(inputDir) || !Directory.Exists(okDir))
            {
                MessageBox.Show(
                    $"Profile \"{_activeProfileName}\": directories must exist.\n\n" +
                    $"Input Dir = [{inputDir}]  exists={Directory.Exists(inputDir)}\n" +
                    $"OK Dir    = [{okDir}]  exists={Directory.Exists(okDir)}\n\n" +
                    $"Open the Profile Manager (Profiles menu) to fix the paths.",
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            txtInputDir.Text = inputDir;
            txtOkDir.Text = okDir;

            fileOperations = new FileOperations(inputDir, fileOpQueuePollMs);
            fileOperations.FileOpEvent += FileOpEventHandler;

            if (inPlaceBrowsing)
                fileOperations.LoadHistory(inputDir);

            timPendingDelete = new System.Windows.Forms.Timer();
            timPendingDelete.Interval = pendingDeletePollMs;
            timPendingDelete.Tick += timPendingDelete_Tick;
            timPendingDelete.Start();

            RefreshItemsListing();

            AdjustColumnWidths();

            loaded = true;
        }

        private void FileOpEventHandler(object sender, FileOpEventArgs e)
        {
            LogMessage(e.Message);
        }
        private bool HasSelection()
        {
            return lstFiles.SelectedItems.Count > 0;
        }

        private void RefreshItemsListing()
        {
            RefreshItemsListingAsync(false);
        }

        private void RefreshItemsListingWithAutoplay()
        {
            RefreshItemsListingAsync(true);
        }

        private FileData[] _fileInfoCache;
        private HashSet<string> _okDirCache;

        private FileData[] GetFileInfos()
        {
            if (noCache || _fileInfoCache == null || _fileInfoCache.Length == 0)
            {
                if (EverythingSearch.IsAvailable)
                {
                    try
                    {
                        _fileInfoCache = EverythingSearch.GetFiles(inputDir);
                        LogMessage($"* Everything index       [ {_fileInfoCache.Length} files ]  query {EverythingSearch.LastQueryMs} ms  enum {EverythingSearch.LastEnumMs} ms");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"* Everything failed      [ {ex.Message} ] — falling back to EnumerateFiles");
                        _fileInfoCache = EnumerateFilesFallback();
                    }
                }
                else
                {
                    _fileInfoCache = EnumerateFilesFallback();
                }
            }
            return _fileInfoCache;
        }

        private FileData[] EnumerateFilesFallback() =>
            new DirectoryInfo(inputDir)
                .EnumerateFiles("*.*", SearchOption.AllDirectories)
                .Select(fi => new FileData(fi))
                .ToArray();

        // Returns paths of inputDir video files that are content-identical to a file already in okDir.
        // Result is cached for the session; invalidated by force-refresh (Shift+F5).
        // Safe to call from a background thread.
        private HashSet<string> FindAlreadySavedInOkDir()
        {
            if (_okDirCache != null) return _okDirCache;

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (inPlaceBrowsing) { _okDirCache = result; return result; }

            var okSizes = new HashSet<long>(
                new DirectoryInfo(okDir)
                    .EnumerateFiles("*.*", SearchOption.AllDirectories)
                    .Select(f => f.Length)
                    .Where(len => len > MIN_FILE_SIZE_BYTES));

            if (okSizes.Count == 0) { _okDirCache = result; return result; }

            foreach (var fi in GetFileInfos())
            {
                if (fileOperations.IsInHistory(fi.FullName)) continue;
                if (fi.Length <= MIN_FILE_SIZE_BYTES) continue;
                if (!videoExtensions.Contains(Path.GetExtension(fi.Name).ToLower())) continue;
                if (!okSizes.Contains(fi.Length)) continue;

                var dupes = DuplicateFinder.FindDuplicates(fi, okDir, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize);
                if (dupes.Count > 0)
                    result.Add(fi.FullName);
            }

            _okDirCache = result;
            return result;
        }

        private (List<ListViewItem> items, long totalSize,
                 Dictionary<string, List<FileData>> dupeGroups,
                 HashSet<string> alreadyInOk,
                 Dictionary<string, List<string>> similarNameGroups) ComputeListData()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var allFiles = GetFileInfos();
            ProfileLog($"  [profile] GetFileInfos      {sw.ElapsedMilliseconds,6} ms  ({allFiles.Length} files)");
            sw.Restart();

            var lstDirs = new List<ListViewItem>();
            long totalSize = 0;
            int itemsInListCount = 0;

            // Pre-group allFiles by directory, sorted so each dir's subdirs form a contiguous run.
            var byDir = allFiles
                .GroupBy(f => f.DirectoryName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
            ProfileLog($"  [profile] byDir group        {sw.ElapsedMilliseconds,6} ms  ({byDir.Count} dirs)");
            sw.Restart();

            // Build subtree file-count cache in O(n log n): sorted keys let us find a dir's
            // entire subtree by scanning forward until the prefix no longer matches — no O(n²) scan.
            var sortedKeys = byDir.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();
            var dirFilesCache = new Dictionary<string, FileData[]>(sortedKeys.Length, StringComparer.OrdinalIgnoreCase);
            for (int ki = 0; ki < sortedKeys.Length; ki++)
            {
                var dirPath = sortedKeys[ki];
                var prefix  = dirPath + Path.DirectorySeparatorChar;
                var subtree = new List<FileData>(byDir[dirPath]);
                for (int kj = ki + 1; kj < sortedKeys.Length; kj++)
                {
                    if (!sortedKeys[kj].StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) break;
                    subtree.AddRange(byDir[sortedKeys[kj]]);
                }
                dirFilesCache[dirPath] = subtree
                    .Where(f => f.Length > MIN_DIR_SIZE_FOR_COUNT_BYTES)
                    .ToArray();
            }
            ProfileLog($"  [profile] subtreeCache       {sw.ElapsedMilliseconds,6} ms");
            sw.Restart();

            foreach (var fi in allFiles)
            {
                if (fi.DirectoryName.IndexOf("_unpa",    StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (fi.DirectoryName.IndexOf("imageset", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (fileOperations.IsInHistory(fi.FullName)) continue;
                if (fi.Length <= MIN_FILE_SIZE_BYTES) continue;

                dirFilesCache.TryGetValue(fi.DirectoryName, out var cachedDirFiles);

                var wod = fileActions.ShouldWorkOnFileOrDir(fi, cachedDirFiles);

                var li = new ListViewItem()
                {
                    Text = wod == WorkOnFileOrDir.Dir ? fi.Directory.Name : fi.Name,
                    Tag = new ListItemTag() { FileInfo = fi, WorkOnFileOrDir = wod }
                };

                totalSize += fi.Length;
                li.SubItems.Add(fi.CreationTime.ToString("u"));
                li.SubItems.Add(fi.Length.ToString("N0"));
                li.SubItems.Add("" + wod);

                lstDirs.Add(li);

                if (++itemsInListCount >= MAX_LIST_ITEMS) break;
            }

            ProfileLog($"  [profile] BuildList         {sw.ElapsedMilliseconds,6} ms  ({lstDirs.Count} items, {dirFilesCache.Count} dirs)");
            sw.Restart();

            // If the displayed set is a subset of the last full computation, groups are still valid.
            var currentPaths = new HashSet<string>(
                lstDirs.Select(li => (li.Tag as ListItemTag).FileInfo.FullName),
                StringComparer.OrdinalIgnoreCase);

            Dictionary<string, List<FileData>> dupeGroups;
            Dictionary<string, List<string>> similarGroups;

            if (noDupeCheck)
            {
                dupeGroups    = new Dictionary<string, List<FileData>>(StringComparer.OrdinalIgnoreCase);
                similarGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }
            else if (_groupsCachedForPaths != null && currentPaths.IsSubsetOf(_groupsCachedForPaths))
            {
                dupeGroups    = _cachedDupeGroups;
                similarGroups = _cachedSimilarGroups;
                ProfileLog($"  [profile] Groups            cached (reused)");
            }
            else
            {
                dupeGroups = DuplicateFinder.FindDuplicateGroupsInList(
                    lstDirs.Select(li => (li.Tag as ListItemTag).FileInfo),
                    videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize);
                ProfileLog($"  [profile] FindDupeGroups    {sw.ElapsedMilliseconds,6} ms  ({dupeGroups.Count} dupes)");
                sw.Restart();

                similarGroups = DuplicateFinder.FindSimilarNameGroups(
                    lstDirs.Select(li => { var tag = li.Tag as ListItemTag; return (tag.FileInfo.FullName, li.Text); }),
                    similarNameMinTokens);
                ProfileLog($"  [profile] FindSimilarNames  {sw.ElapsedMilliseconds,6} ms  ({similarGroups.Count} groups)");

                _cachedDupeGroups      = dupeGroups;
                _cachedSimilarGroups   = similarGroups;
                _groupsCachedForPaths  = currentPaths;
            }

            sw.Restart();
            var alreadyInOk = noDupeCheck
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : FindAlreadySavedInOkDir();
            ProfileLog($"  [profile] FindAlreadyInOk   {sw.ElapsedMilliseconds,6} ms  ({alreadyInOk.Count} matches)");

            return (lstDirs, totalSize, dupeGroups, alreadyInOk, similarGroups);
        }

        private async void RefreshItemsListingAsync(bool autoplay = false)
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            int selectedIndex = HasSelection() ? lstFiles.SelectedIndices[0] : -1;

            LogMessage($"* Refresh dir content    [ {inputDir} ]");
            prgLoading.Visible = true;

            List<ListViewItem> lstDirs;
            long totalSize;

            try
            {
                (lstDirs, totalSize, _dupeGroups, _alreadyInOkPaths, _similarNameGroups) =
                    await Task.Run(() => ComputeListData());
            }
            catch (IOException ex)
            {
                LogMessage($"* Oops [{ex.Message}] — retrying...");
                await Task.Delay(ioRetryDelayMs);
                try
                {
                    (lstDirs, totalSize, _dupeGroups, _alreadyInOkPaths, _similarNameGroups) =
                        await Task.Run(() => ComputeListData());
                }
                catch (Exception ex2)
                {
                    LogMessage($"* Retry failed [{ex2.Message}]");
                    prgLoading.Visible = false;
                    _isRefreshing = false;
                    return;
                }
            }

            foreach (var p in _alreadyInOkPaths)
                LogMessage($"* Already in OK          [ {Path.GetFileName(p)} ]");

            // Build current path set once for peer filtering (groups may contain stale peers
            // from a previous larger list when cached results are reused).
            var currentPaths = new HashSet<string>(
                lstDirs.Select(li => (li.Tag as ListItemTag).FileInfo.FullName),
                StringComparer.OrdinalIgnoreCase);

            if (ckShowDupesOnly.Checked)
                lstDirs.RemoveAll(li =>
                {
                    var path = (li.Tag as ListItemTag).FileInfo.FullName;
                    return !(_dupeGroups.TryGetValue(path, out var dp) && dp.Any(p => currentPaths.Contains(p.FullName)))
                        && !_alreadyInOkPaths.Contains(path)
                        && !(_similarNameGroups.TryGetValue(path, out var sp) && sp.Any(p => currentPaths.Contains(p)));
                });

            foreach (var li in lstDirs)
            {
                var path = (li.Tag as ListItemTag).FileInfo.FullName;
                if (_alreadyInOkPaths.Contains(path))
                {
                    li.BackColor = colorAlreadySaved;
                    li.ForeColor = Color.White;
                }
                else if (_dupeGroups.TryGetValue(path, out var dupePeers) && dupePeers.Any(p => currentPaths.Contains(p.FullName)))
                {
                    li.BackColor = colorDupe;
                    li.ForeColor = Color.Black;
                }
                else if (_similarNameGroups.TryGetValue(path, out var simPeers) && simPeers.Any(p => currentPaths.Contains(p)))
                {
                    li.BackColor = colorSimilarName;
                    li.ForeColor = Color.Black;
                }
            }

            lstFiles.BeginUpdate();
            lstFiles.Items.Clear();

            if (sortDirection == 1)
            {
                lstDirs.Sort(new DateTimeSortDesc());
                lstFiles.Items.AddRange(lstDirs.ToArray());
            }
            else if (sortDirection == 3)
            {
                lstFiles.Items.AddRange(lstDirs.OrderBy(_ => _.SubItems[0].Text).ToArray());
            }
            else if (sortDirection == 7)
            {
                var rand = new Random(DateTime.Now.Millisecond);
                lstFiles.Items.AddRange(lstDirs.OrderBy(_ => (rand.Next() * rand.Next())).ToArray());
            }

            if (lstFiles.Items.Count > 0)
                lstFiles.Items[0].Selected = true;

            lstFiles.EndUpdate();

            lastTotalSize = totalSize;
            UpdateTitle(totalSize);

            prgLoading.Visible = false;

            lstFiles.Focus();
            if (selectedIndex > 0 && selectedIndex < lstFiles.Items.Count)
                lstFiles.Items[selectedIndex].Selected = true;
            lstFiles.Focus();

            _isRefreshing = false;

            if (autoplay && ckAutoplay.Checked)
                LaunchItem(false, ckFullScreen.Checked);
        }

        private string SortLabel() =>
            sortDirection == 1 ? "DAT" : sortDirection == 3 ? "NAM" : "RND";

        private void UpdateTitle(long totalSize)
        {
            ulong inputDirFreeSpace, inputDirTotalSpace;
            Util.DriveFreeBytes(inputDir, out inputDirFreeSpace, out inputDirTotalSpace);

            ulong okDirFreeSpace, okDirTotalSpace;
            Util.DriveFreeBytes(okDir, out okDirFreeSpace, out okDirTotalSpace);

            var pendingPart = pendingDeletes.Count > 0 ? $" | DEL pending {pendingDeletes.Count}" : "";
            Text = String.Format("{0} | {1} Entries | {2} | {3} | {4} | {5}{6}",
                inputDir,
                lstFiles.Items.Count,
                totalSize.ToString("N0"),
                inputDirFreeSpace.ToString("N0") + " / " + inputDirTotalSpace.ToString("N0"),
                okDirFreeSpace.ToString("N0") + " / " + okDirTotalSpace.ToString("N0"),
                SortLabel(),
                pendingPart
                );
        }

        private static void DeleteFileIgnoreReadOnly(string path)
        {
            var fi = new FileInfo(path);
            if (fi.IsReadOnly) fi.IsReadOnly = false;
            fi.Delete();
        }

        private static void DeleteDirectoryIgnoreReadOnly(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(file);
                if (fi.IsReadOnly) fi.IsReadOnly = false;
            }
            Directory.Delete(path, true);
        }

        private void DeleteItem()
        {
            if (HasSelection())
                DeleteItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
        }

        private void DeleteItem(ListItemTag lit)
        {
            if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
            {
                // Use cached file list to avoid a disk scan just for the file count check.
                var dirPath = lit.FileInfo.DirectoryName;
                var countFiles = _fileInfoCache != null
                    ? _fileInfoCache.Count(f => f.DirectoryName.StartsWith(dirPath, StringComparison.OrdinalIgnoreCase)
                                             && f.Length > MIN_DIR_SIZE_FOR_COUNT_BYTES)
                    : lit.FileInfo.Directory.GetFiles("*.*", SearchOption.AllDirectories)
                                            .Count(f => f.Length > MIN_DIR_SIZE_FOR_COUNT_BYTES);

                if (countFiles > MAX_FILES_IN_DIR_TO_DELETE)
                {
                    LogMessage($"* NOT Deleting Directory [ {lit.FileInfo.Directory.FullName} ] too many files in it {countFiles}");

                    ExploreItem();

                    return;
                }
            }

            fileOperations.AddToHistory(lit.FileInfo.FullName);
            pendingDeletes.Add(new PendingDelete { Item = lit, QueuedAt = DateTime.Now });

            var label = lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir ? lit.FileInfo.Directory.Name : lit.FileInfo.Name;
            LogMessage($"* Pending delete         [ {label} ] Ctrl+Z to cancel (60s)");

            var dupes = _fileInfoCache != null && _fileInfoCache.Length > 0
                ? DuplicateFinder.FindDuplicatesInList(lit.FileInfo, _fileInfoCache, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize)
                : DuplicateFinder.FindDuplicates(lit.FileInfo, inputDir, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize);
            foreach (var dupe in dupes)
            {
                if (pendingDeletes.Any(p => string.Equals(p.Item.FileInfo.FullName, dupe.FullName, StringComparison.OrdinalIgnoreCase))) continue;
                var dupeLit = new ListItemTag { FileInfo = dupe, WorkOnFileOrDir = WorkOnFileOrDir.File };
                fileOperations.AddToHistory(dupe.FullName);
                pendingDeletes.Add(new PendingDelete { Item = dupeLit, QueuedAt = DateTime.Now });
                LogMessage($"* Pending delete dupe    [ {dupe.Name} ] Ctrl+Z to cancel (60s)");
            }

            RefreshItemsListingWithAutoplay();
        }

        private void ExecuteDelete(ListItemTag lit)
        {
            bool ok = false;
            if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
            {
                LogMessage($"* Deleting Directory     [ {lit.FileInfo.Directory.FullName} ]");
                try { DeleteDirectoryIgnoreReadOnly(lit.FileInfo.Directory.FullName); ok = true; }
                catch (Exception ex) { LogMessage($"* Delete failed          [ {ex.Message} ]"); }
            }
            else
            {
                LogMessage($"* Deleting File          [ {lit.FileInfo.FullName} ]");
                try { DeleteFileIgnoreReadOnly(lit.FileInfo.FullName); ok = true; }
                catch (Exception ex) { LogMessage($"* Delete failed          [ {ex.Message} ]"); }
            }

            if (!ok)
            {
                fileOperations.RemoveFromHistory(lit.FileInfo.FullName);
                RefreshItemsListing();
            }
        }

        private void timPendingDelete_Tick(object sender, EventArgs e)
        {
            var toExecute = pendingDeletes.Where(p => (DateTime.Now - p.QueuedAt).TotalSeconds >= 60).ToList();
            foreach (var p in toExecute)
            {
                pendingDeletes.Remove(p);
                ExecuteDelete(p.Item);
            }
            if (toExecute.Count > 0 || pendingDeletes.Count > 0)
                UpdateTitle(lastTotalSize);
        }

        private void DeleteParentItem()
        {
            if (HasSelection())
                DeleteParentItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
        }

        private void DeleteParentItem(ListItemTag lit)
        {
            if (DialogResult.Yes == MessageBox.Show(
                $"Actually delete \n\n{lit.FileInfo.Directory.FullName}\n\n?", "Confirm",
                MessageBoxButtons.YesNoCancel))
            {
                LogMessage($"* Deleting Directory     [ {lit.FileInfo.Directory.FullName} ]");
                try
                {
                    DeleteDirectoryIgnoreReadOnly(lit.FileInfo.Directory.FullName);
                    RefreshItemsListingWithAutoplay();
                }
                catch (Exception ex)
                {
                    LogMessage($"* Delete failed          [ {ex.Message} ]");
                }
            }
        }

        private void RenameItem()
        {
            if (HasSelection())
            {
                RenameItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void RenameItem(ListItemTag lit)
        {
            if (File.Exists(lit.FileInfo.FullName)
                &&
                !File.Exists(txtFileName.Text))
            {
                File.Move(lit.FileInfo.FullName, txtFileName.Text);
                LogMessage($"* File Renamed to        [ {txtFileName.Text} ]");
            }
        }

        private void ExploreItem()
        {
            if (HasSelection())
            {
                ExploreItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void ExploreItem(ListItemTag lit)
        {
            string tgt;

            if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
            {
                tgt = lit.FileInfo.Directory.FullName;
            }
            else
            {
                tgt = lit.FileInfo.FullName;
            }

            var piTC = new ProcessStartInfo
            {
                FileName  = ConfigurationManager.AppSettings["TC_EXE"],
                Arguments = $"/O 0 /T /L=\"{tgt}\"",
                UseShellExecute = false,
            };

            Process.Start(piTC);
        }

        private void SIRItem(bool noSuffix)
        {
            if (HasSelection())
            {
                ExploreItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
                SIRItem(lstFiles.SelectedItems[0].Tag as ListItemTag, noSuffix);
            }
        }

        private void SIRItem(ListItemTag lit, bool noSuffix)
        {
            if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
            {

                var pi = new ProcessStartInfo(
                    "cmd.exe",
                    "/C \"" + scriptSabnzbd + "\" \"" + lit.FileInfo.Directory.FullName + "\" " +
                    (!noSuffix ? relSuffix : "")
                    );

                pi.WindowStyle = ProcessWindowStyle.Maximized;

                Process.Start(pi);
            }
            else
            {
                LogMessage($"* Operation not available for File [ {lit.FileInfo.FullName} ]");

            }
        }

        private void ListContent()
        {
            if (HasSelection())
            {
                ListContent(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void ListContent(ListItemTag lit)
        {
            LogMessage("<<< \r\n");

            if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
            {
                var di = lit.FileInfo.Directory;
                var ff = di.GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var f in ff)
                {
                    LogMessage(string.Format("{0:N0}   ", f.Length).PadLeft(18, ' ') + " [ " + f.Name + " ]");

                    if (!inPlaceBrowsing && util.IsDangerousExtension(f.Name.ToLower()))
                    {
                        f.Delete();
                    }
                }

                LogMessage($"D [ {di.Name} ] {ff.Count()} entries\r\n");
                txtSelectedDir.Text = di.FullName;
                txtFileName.Text = lit.FileInfo.FullName;
                lblFileName.Text = lit.FileInfo.Directory.Name;
            }
            else
            {
                // working on file
                LogMessage(string.Format("{0:N0}   ", lit.FileInfo.Length).PadLeft(18, ' ') + " [ " + lit.FileInfo.Name + " ]");
                txtSelectedDir.Text = lit.FileInfo.Directory.FullName;
                txtFileName.Text = lit.FileInfo.FullName;
                lblFileName.Text = lit.FileInfo.Name;
            }

            LogMessage($"F [ {lit.FileInfo.FullName} ] \r\n");

            if (_dupeGroups.TryGetValue(lit.FileInfo.FullName, out var peers))
            {
                LogMessage($"  DUPE peers ({peers.Count}):");
                foreach (var peer in peers)
                    LogMessage($"    {peer.Length,18:N0}  [ {peer.FullName} ]");
            }

            if (_alreadyInOkPaths.Contains(lit.FileInfo.FullName))
            {
                var okMatches = DuplicateFinder.FindDuplicates(lit.FileInfo, okDir, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize);
                LogMessage($"  ALREADY IN OK ({okMatches.Count}):");
                foreach (var m in okMatches)
                    LogMessage($"    {m.Length,18:N0}  [ {m.FullName} ]");
            }

            if (_similarNameGroups.TryGetValue(lit.FileInfo.FullName, out var similarPeers))
            {
                LogMessage($"  SIMILAR NAME ({similarPeers.Count}):");
                foreach (var peerPath in similarPeers)
                    LogMessage($"    [ {peerPath} ]");
            }

            LogMessage(">>> \r\n");
        }

        private void ckShowDupesOnly_CheckedChanged(object sender, EventArgs e)
        {
            RefreshItemsListing();
        }

        private static string RegSubKey(string dir) =>
            @"Software\LezyFileBrowser\" +
            dir.Replace(':', '_').Replace('\\', '_').Replace('/', '_');

        private void LoadCheckboxState()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegSubKey(inputDir));
            if (key != null)
            {
                ckFullScreen.Checked   = (string)key.GetValue("FullScreen",   "0") == "1";
                ckOnTop.Checked        = (string)key.GetValue("OnTop",        "0") == "1";
                ckKeepFocus.Checked    = (string)key.GetValue("KeepFocus",    "1") == "1";
                ckAutoplay.Checked     = (string)key.GetValue("Autoplay",     "0") == "1";
                ckShowDupesOnly.Checked = (string)key.GetValue("ShowDupesOnly", "0") == "1";
            }
            else
            {
                // First run for this inputDir — derive FullScreen from time-of-day window
                int now      = DateTime.Now.Hour * 100 + DateTime.Now.Minute;
                int fsBefore = int.Parse(ConfigurationManager.AppSettings["FS_HHMM_BEFORE"]);
                int fsAfter  = int.Parse(ConfigurationManager.AppSettings["FS_HHMM_AFTER"]);
                ckFullScreen.Checked = now < fsBefore || now >= fsAfter;
            }
        }

        private void SaveCheckboxState()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegSubKey(inputDir));
            key.SetValue("FullScreen",    ckFullScreen.Checked    ? "1" : "0");
            key.SetValue("OnTop",         ckOnTop.Checked         ? "1" : "0");
            key.SetValue("KeepFocus",     ckKeepFocus.Checked     ? "1" : "0");
            key.SetValue("Autoplay",      ckAutoplay.Checked      ? "1" : "0");
            key.SetValue("ShowDupesOnly", ckShowDupesOnly.Checked ? "1" : "0");
        }

        private void LaunchItem(bool useAltPlayer, bool fullScreen)
        {
            if (HasSelection())
            {
                fileActions.LaunchFile((lstFiles.SelectedItems[0].Tag as ListItemTag).FileInfo, useAltPlayer, fullScreen, ckOnTop.Checked, ckKeepFocus.Checked, this.Handle);
            }
        }

        private void SearchItem()
        {
            if (HasSelection())
            {
                ExploreItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
                SearchItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void SearchItem(ListItemTag lit)
        {
            var s = lit.FileInfo.Directory.Name;

            var search = WebUtility.UrlEncode(s);

            Process.Start(
                 new ProcessStartInfo
                 {
                     UseShellExecute = true,
                     FileName = browserExe,
                     Arguments = btSearchUrl1 + search
                 });

            Process.Start(
                 new ProcessStartInfo
                 {
                     UseShellExecute = true,
                     FileName = browserExe,
                     Arguments = btSearchUrl2 + search
                 });
        }

        private void SaveForLaterSearch()
        {
            if (HasSelection())
            {
                SaveForLaterSearch(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void SaveForLaterSearch(ListItemTag lit)
        {
            var fName = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LZ_FM_TDLD.txt");

            var s = lit.FileInfo.Directory.Name + "  -  " + lit.FileInfo.Name;

            File.AppendAllLines(fName, new string[] { s });

            LogMessage($"S [ {s} ] \r\n");
        }

        private void SaveItem()
        {
            if (HasSelection())
            {
                SaveItem(lstFiles.SelectedItems[0].Tag as ListItemTag);
            }
        }

        private void SaveItem(ListItemTag lit)
        {
            fileOperations.AddToHistory(lit.FileInfo.FullName);

            var swSave = System.Diagnostics.Stopwatch.StartNew();
            // Use cached file list when available to avoid a full directory scan on every save.
            var dupes = _fileInfoCache != null && _fileInfoCache.Length > 0
                ? DuplicateFinder.FindDuplicatesInList(lit.FileInfo, _fileInfoCache, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize)
                : DuplicateFinder.FindDuplicates(lit.FileInfo, inputDir, videoExtensions, MIN_FILE_SIZE_BYTES, dupeCheckHeadMb, dupeCheckTailMb, dupeCheckBufSize);
            ProfileLog($"  [profile] SaveItem.FindDupes {swSave.ElapsedMilliseconds,6} ms  ({dupes.Count} dupes found)");
            foreach (var dupe in dupes)
            {
                LogMessage($"* Deleting dupe          [ {dupe.Name} ]");
                fileOperations.AddToHistory(dupe.FullName);
                try { DeleteFileIgnoreReadOnly(dupe.FullName); }
                catch (Exception ex) { LogMessage($"* Dupe delete failed     [ {ex.Message} ]"); }
            }

            if (!inPlaceBrowsing)
            {
                if (lit.WorkOnFileOrDir == WorkOnFileOrDir.Dir)
                {
                    LogMessage($"* to                     [ {okDir + lit.FileInfo.Directory.Name} ]");
                    LogMessage($"* Saving Directory       [ {lit.FileInfo.Directory.FullName} ]");

                    fileOperations.Enqueue(
                        new DirMoveOperation()
                        {
                            Source = lit.FileInfo.Directory.FullName,
                            Destination = okDir + lit.FileInfo.Directory.Name,
                            MoveWithRobocopy = moveWithRobocopy
                        });
                }
            }

            RefreshItemsListingWithAutoplay();
        }

        private void LogMessage(string v)
        {
            sbLogs.Add(v);
        }

        private void ProfileLog(string v)
        {
            if (profileLog) sbLogs.Add(v);
        }

        private void lstDirectories_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                if (pendingDeletes.Count > 0)
                {
                    var last = pendingDeletes[pendingDeletes.Count - 1];
                    pendingDeletes.RemoveAt(pendingDeletes.Count - 1);
                    fileOperations.RemoveFromHistory(last.Item.FileInfo.FullName);

                    var label = last.Item.WorkOnFileOrDir == WorkOnFileOrDir.Dir
                        ? last.Item.FileInfo.Directory.Name
                        : last.Item.FileInfo.Name;
                    LogMessage($"* Delete cancelled       [ {label} ] ({pendingDeletes.Count} remaining)");
                    RefreshItemsListing();
                }

                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                LaunchItem(false, ckFullScreen.Checked);

                return;
            }

            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back || e.KeyCode == Keys.OemBackslash || e.KeyCode == Keys.Oem5)
            {
                e.SuppressKeyPress = true;

                if (e.Shift)
                {
                    DeleteParentItem();
                }
                else
                {
                    DeleteItem();
                }

                return;
            }

            if (e.KeyCode == Keys.Space)
            {
                SaveItem();

                return;
            }

            if (e.KeyCode == Keys.F3)
            {
                ExploreItem();

                return;
            }

            if (e.KeyCode == Keys.F4)
            {
                SIRItem(e.Shift);

                return;
            }

            if (e.KeyCode == Keys.F5)
            {
                if (e.Shift)
                {
                    _fileInfoCache        = null;
                    _okDirCache           = null;
                    _groupsCachedForPaths = null;
                }

                RefreshItemsListing();

                return;
            }

            if (e.KeyCode == Keys.F6)
            {
                SearchItem();

                return;
            }

            if (e.KeyCode == Keys.F7)
            {
                if (sortDirection == 1) sortDirection = 3;
                else if (sortDirection == 3) sortDirection = 7;
                else if (sortDirection == 7) sortDirection = 1;

                LogMessage($"* Sort                   [ {SortLabel()} ]");
                RefreshItemsListing();

                return;
            }

            if (e.KeyCode == Keys.F9)
            {
                // launch with alternative player
                LaunchItem(true, ckFullScreen.Checked);

                return;
            }

            if (e.KeyCode == Keys.F11)
            {
                SaveForLaterSearch();

                return;
            }
        }

        private void lstDirectories_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListContent();
        }

        private void timUIUpdate_Tick(object sender, EventArgs e)
        {
            if (sbLogs.Count > lastCount)
            {
                lastCount = sbLogs.Count;

                StringBuilder sb = new StringBuilder();
                IEnumerable<string> logM = sbLogs.ToArray().Reverse();

                foreach (string sbLogz in logM)
                {
                    sb.AppendLine(sbLogz);
                }

                txtLog.Text = sb.ToString();
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCheckboxState();
            if (pendingDeletes.Count > 0)
            {
                var toDelete = pendingDeletes.ToList();
                pendingDeletes.Clear();
                foreach (var p in toDelete)
                    ExecuteDelete(p.Item);
            }

            if (inPlaceBrowsing)
                fileOperations.SaveHistory();
        }

        private void btnDeleteDir_Click(object sender, EventArgs e)
        {
            DeleteParentItem();
        }

        private void btnKillMp_Click(object sender, EventArgs e)
        {
            var names = ConfigurationManager.AppSettings["PLAYER_PROCESS_NAMES"]
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var name in names)
                foreach (var p in System.Diagnostics.Process.GetProcessesByName(name.Trim()))
                    try { p.Kill(); } catch { }
        }

        private void btnRename_Click(object sender, EventArgs e)
        {
            RenameItem();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshItemsListing();
        }

        private void lstFiles_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                RefreshItemsListing();
        }

        private void lstFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            LaunchItem(false, ckFullScreen.Checked);
        }

        private void btnLaunchItem_Click(object sender, EventArgs e)
        {
            LaunchItem(false, ckFullScreen.Checked);
        }

        // ── Profile support ───────────────────────────────────────────────

        private void ApplyProfile(Profile p)
        {
            _activeProfileName = p.Name;
            inputDir           = p.InputDir;
            okDir              = p.OkDir;
            sortDirection      = p.SortDir;
            inPlaceBrowsing    = p.InplaceBrowsing;
            noCache            = p.NoCache;
            noDupeCheck        = p.NoDupeCheck;
            profileLog         = p.ProfileLog;
            MAX_LIST_ITEMS     = p.MaxListItems;
            MIN_FILE_SIZE_MB   = p.MinFileSizeMb;
            // moveWithRobocopy is resolved later in MainForm_Load (mount-point detection)
        }

        private void BuildMenuStrip()
        {
            _menuStrip        = new MenuStrip();
            _menuItemProfiles = new ToolStripMenuItem("Profiles");
            _menuItemProfiles.DropDownOpening += (s, e) => RefreshProfilesMenu();
            _menuStrip.Items.Add(_menuItemProfiles);
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;
        }

        private void RefreshProfilesMenu()
        {
            _menuItemProfiles.DropDownItems.Clear();

            var manage = new ToolStripMenuItem("Manage Profiles...");
            manage.Font  = new Font(manage.Font, FontStyle.Bold);
            manage.Click += (s, e) =>
            {
                using var dlg = new ProfileManagerForm(_activeProfileName, startupMode: false);
                dlg.ShowDialog(this);
            };
            _menuItemProfiles.DropDownItems.Add(manage);
            _menuItemProfiles.DropDownItems.Add(new ToolStripSeparator());

            foreach (var p in ProfileRegistry.LoadAll())
            {
                var isCurrent = string.Equals(p.Name, _activeProfileName, StringComparison.OrdinalIgnoreCase);
                var item = new ToolStripMenuItem(isCurrent ? $"● {p.Name}" : p.Name);
                if (isCurrent) item.Font = new Font(item.Font, FontStyle.Bold);
                if (!string.IsNullOrEmpty(p.Description))
                    item.ToolTipText = p.Description;

                var captured = p.Name;
                item.Click += (s, e) => LaunchProfileNewWindow(captured);
                _menuItemProfiles.DropDownItems.Add(item);
            }
        }

        private void LaunchProfileNewWindow(string profileName)
        {
            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = Application.ExecutablePath,
                    Arguments       = $"\"{profileName}\"",
                    UseShellExecute = false,
                }
            };
            proc.Start();
        }
    }

    class PendingDelete
    {
        public ListItemTag Item { get; set; }
        public DateTime QueuedAt { get; set; }
    }
}
