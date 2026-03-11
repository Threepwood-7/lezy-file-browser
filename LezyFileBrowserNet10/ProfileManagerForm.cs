using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LezyFileBrowser
{
    /// <summary>
    /// Profile management dialog. Two modes:
    ///   startupMode=true  — shown at first run; "Open Profile" button loads the profile into
    ///                        the calling MainForm (SelectedProfile is set, DialogResult=OK).
    ///   startupMode=false — shown from the Profiles menu while a profile is already running;
    ///                        "Launch" / "Launch &amp; Exit" open new instances.
    /// </summary>
    public class ProfileManagerForm : Form
    {
        // ── Left panel ──────────────────────────────────────────────────────
        private ListView  _list;
        private Button    _btnNew, _btnClone, _btnDelete;

        // ── Right panel – fields ────────────────────────────────────────────
        private TextBox       _txtName, _txtDesc;
        private TextBox       _txtInputDir, _txtOkDir;
        private Button        _btnBrowseInput, _btnBrowseOk;
        private ComboBox      _cmbSort;
        private CheckBox      _chkInplace;
        private NumericUpDown _nudMinSize;
        private NumericUpDown _nudMaxItems;
        private CheckBox      _chkMaxUnlimited;
        private ComboBox      _cmbMoveMethod;
        private CheckBox      _chkNoCache, _chkNoDupeCheck, _chkProfileLog;
        private Button        _btnSave;

        // ── Bottom bar ──────────────────────────────────────────────────────
        private Button _btnLaunch, _btnLaunchExit, _btnOpenProfile, _btnClose;

        // ── State ───────────────────────────────────────────────────────────
        private bool   _dirty;
        private string _originalName;          // name when profile was loaded into editor
        private bool   _loading;              // suppress MarkDirty during LoadIntoEditor
        private readonly string _currentProfileName;  // running profile, for bold row
        private readonly bool   _startupMode;
        private ToolTip _tip;

        /// <summary>Set when user clicks Open Profile in startup mode.</summary>
        public Profile SelectedProfile { get; private set; }

        // ════════════════════════════════════════════════════════════════════
        public ProfileManagerForm(string currentProfileName, bool startupMode = false)
        {
            _currentProfileName = currentProfileName;
            _startupMode        = startupMode;

            Text            = "Profile Manager";
            Size            = new Size(1060, 680);
            MinimumSize     = new Size(740, 520);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Font            = new Font("Segoe UI", 9f);

            _tip = new ToolTip { AutoPopDelay = 9000, InitialDelay = 500, ReshowDelay = 300 };

            BuildLayout();
            RefreshList(selectName: currentProfileName);
        }

        // ════════════════════════════════ LAYOUT ════════════════════════════

        private void BuildLayout()
        {
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 52 };
            bottomBar.Paint += (s, e) =>
                e.Graphics.DrawLine(SystemPens.ControlDark, 0, 0, bottomBar.Width, 0);
            BuildBottomBar(bottomBar);

            var split = new SplitContainer
            {
                Dock          = DockStyle.Fill,
                SplitterWidth = 5,
                FixedPanel    = FixedPanel.Panel1,
            };
            // MinSize and SplitterDistance require real pixel dimensions — defer to Load
            Load += (s, e) =>
            {
                split.Panel1MinSize  = 240;
                split.Panel2MinSize  = 380;
                split.SplitterDistance = 360;
            };

            BuildLeftPanel(split.Panel1);
            BuildRightPanel(split.Panel2);

            Controls.Add(split);
            Controls.Add(bottomBar);
        }

        private void BuildBottomBar(Panel p)
        {
            if (_startupMode)
            {
                _btnOpenProfile = MakeButton("Open Profile", 120, new Point(8, 10));
                _btnOpenProfile.Font = new Font(_btnOpenProfile.Font, FontStyle.Bold);
                _tip.SetToolTip(_btnOpenProfile, "Load the selected profile into this window.");
                _btnOpenProfile.Click += BtnOpenProfile_Click;
                p.Controls.Add(_btnOpenProfile);
            }
            else
            {
                _btnLaunch = MakeButton("Launch (new window)", 148, new Point(8, 10));
                _tip.SetToolTip(_btnLaunch, "Open the selected profile in a new application window.");
                _btnLaunch.Click += (s, e) => LaunchSelected(exitCurrent: false);

                _btnLaunchExit = MakeButton("Launch && Exit this", 148, new Point(162, 10));
                _tip.SetToolTip(_btnLaunchExit, "Open the selected profile in a new window, then close this window once the new one is ready.");
                _btnLaunchExit.Click += (s, e) => LaunchSelected(exitCurrent: true);

                p.Controls.AddRange(new Control[] { _btnLaunch, _btnLaunchExit });
            }

            _btnClose = MakeButton("Close", 90, Point.Empty);
            _btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            _btnClose.Click += (s, e) => Close();
            p.Controls.Add(_btnClose);
            p.Resize += (s, e) =>
            {
                _btnClose.Location = new Point(p.Width - _btnClose.Width - 10, 10);
            };
            // Trigger once to position correctly
            _btnClose.Location = new Point(Math.Max(10, Size.Width - _btnClose.Width - 10 - 16), 10);
        }

        private void BuildLeftPanel(SplitterPanel p)
        {
            var header = MakeHeader("PROFILES");

            _list = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                MultiSelect   = false,
                HideSelection = false,
                HeaderStyle   = ColumnHeaderStyle.Nonclickable,
                Font          = new Font("Segoe UI", 9f),
            };
            _list.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Name",        Width = 180 },
                new ColumnHeader { Text = "Description", Width = 160 },
            });
            _list.SelectedIndexChanged += List_SelectedIndexChanged;
            _list.KeyDown              += List_KeyDown;
            _list.MouseDoubleClick     += List_MouseDoubleClick;

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 38 };
            _btnNew    = MakeButton("New",    64, new Point(4,   6));
            _btnClone  = MakeButton("Clone",  64, new Point(72,  6));
            _btnDelete = MakeButton("Delete", 64, new Point(140, 6));
            _btnNew.Click    += BtnNew_Click;
            _btnClone.Click  += BtnClone_Click;
            _btnDelete.Click += BtnDelete_Click;
            btnPanel.Controls.AddRange(new Control[] { _btnNew, _btnClone, _btnDelete });

            p.Controls.Add(_list);
            p.Controls.Add(header);
            p.Controls.Add(btnPanel);
        }

        private void BuildRightPanel(SplitterPanel p)
        {
            var header = MakeHeader("PROFILE SETTINGS");

            var saveBar = new Panel { Dock = DockStyle.Bottom, Height = 42 };
            saveBar.Paint += (s, e) =>
                e.Graphics.DrawLine(SystemPens.ControlDark, 0, 0, saveBar.Width, 0);
            _btnSave = MakeButton("Save", 90, new Point(6, 8));
            _btnSave.Font    = new Font(_btnSave.Font, FontStyle.Bold);
            _btnSave.Enabled = false;
            _tip.SetToolTip(_btnSave, "Save changes to the registry. Ctrl+S.");
            _btnSave.Click += BtnSave_Click;
            saveBar.Controls.Add(_btnSave);

            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(8, 6, 8, 6),
            };

            var fields = BuildFieldsPanel();
            // Let fields grow with panel width
            fields.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            scroll.Controls.Add(fields);
            scroll.Resize += (s, e) => fields.Width = scroll.ClientSize.Width - 16;

            p.Controls.Add(scroll);
            p.Controls.Add(header);
            p.Controls.Add(saveBar);

            // Ctrl+S shortcut
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S && _btnSave.Enabled)
                    BtnSave_Click(this, EventArgs.Empty);
            };
        }

        // ─── Fields panel ───────────────────────────────────────────────────

        private Panel BuildFieldsPanel()
        {
            var panel = new Panel { AutoSize = false };

            int y = 4;
            const int LW  = 142;   // label column width
            const int CX  = 148;   // control X (label + gap)
            const int RH  = 26;    // control row height
            const int GAP = 5;     // gap between rows

            // helpers ──────────────────────────────────────────────────────

            void AddSectionHeader(string title)
            {
                y += 8;
                var lbl = new Label
                {
                    Text      = title,
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    ForeColor = Color.DimGray,
                    Location  = new Point(0, y),
                    Size      = new Size(900, 17),
                };
                var line = new Panel
                {
                    BackColor = Color.Silver,
                    Location  = new Point(0, y + 19),
                    Size      = new Size(900, 1),
                };
                panel.Controls.Add(lbl);
                panel.Controls.Add(line);
                y += 27;
            }

            void AddRow(string labelText, string tip, Control ctrl)
            {
                var lbl = new Label
                {
                    Text      = labelText,
                    TextAlign = ContentAlignment.MiddleRight,
                    Location  = new Point(0, y),
                    Size      = new Size(LW, RH),
                };
                if (!string.IsNullOrEmpty(tip)) { _tip.SetToolTip(lbl, tip); _tip.SetToolTip(ctrl, tip); }
                ctrl.Location = new Point(CX, y + (RH - ctrl.Height) / 2);
                panel.Controls.Add(lbl);
                panel.Controls.Add(ctrl);
                y += RH + GAP;
            }

            // ── Identity ──────────────────────────────────────────────────
            const string TIP_NAME = "Unique profile name. Also used as the command-line argument:\n  LezyFileBrowserNet10.exe \"ProfileName\"\nChanging the name and saving renames the profile in the registry.";
            const string TIP_DESC = "Optional free-form description shown in the profiles list.";

            _txtName = new TextBox { Height = RH, Width = 300 };
            _txtName.TextChanged += MarkDirty;
            AddRow("Name:", TIP_NAME, _txtName);

            _txtDesc = new TextBox { Height = RH, Width = 420 };
            _txtDesc.TextChanged += MarkDirty;
            AddRow("Description:", TIP_DESC, _txtDesc);

            // ── Directories ───────────────────────────────────────────────
            AddSectionHeader("Directories");

            const string TIP_INDIR = "Source directory to browse. Files from this directory are shown in the list.";
            const string TIP_OKDIR = "Destination directory. Pressing Space moves the selected item here.";

            _txtInputDir = MakeDirTextBox();
            _txtInputDir.TextChanged += (s, e) =>
            {
                if (_chkInplace != null && _chkInplace.Checked)
                    _txtOkDir.Text = _txtInputDir.Text;
                MarkDirty(s, e);
            };
            _btnBrowseInput = MakeBrowseButton(_txtInputDir);
            AddRow("Input Dir:", TIP_INDIR, MakeDirRow(_txtInputDir, _btnBrowseInput));

            _txtOkDir = MakeDirTextBox();
            _txtOkDir.TextChanged += MarkDirty;
            _btnBrowseOk = MakeBrowseButton(_txtOkDir);
            AddRow("OK Dir:", TIP_OKDIR, MakeDirRow(_txtOkDir, _btnBrowseOk));

            // ── Behavior ──────────────────────────────────────────────────
            AddSectionHeader("Behavior");

            const string TIP_INPLACE  = "When checked, OK Dir equals Input Dir. Space marks items as done in-place — nothing is moved.";
            const string TIP_SORT     = "Initial sort order when the profile is loaded.\n• Date (newest first) — sort by last-write time, descending.\n• Name (A → Z) — alphabetical ascending.\n• Random — shuffle on every refresh.";
            const string TIP_MINSIZE  = "Files smaller than this threshold are hidden from the list.";
            const string TIP_MAXITEMS = "Maximum items shown in the list. Reduce to improve performance on very large directories. 'Unlimited' shows everything.";

            _chkInplace = new CheckBox { Text = "Same as Input Dir — no move", AutoSize = true };
            _chkInplace.CheckedChanged += ChkInplace_CheckedChanged;
            _tip.SetToolTip(_chkInplace, TIP_INPLACE);
            AddRow("In-place:", TIP_INPLACE, _chkInplace);

            _cmbSort = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 210 };
            _cmbSort.Items.AddRange(new object[] { "Date (newest first)", "Name (A → Z)", "Random" });
            _cmbSort.SelectedIndexChanged += MarkDirty;
            AddRow("Sort:", TIP_SORT, _cmbSort);

            _nudMinSize = new NumericUpDown { Width = 80, Minimum = 0, Maximum = 999999, DecimalPlaces = 0 };
            _nudMinSize.ValueChanged += MarkDirty;
            var minSizeRow = new Panel { Size = new Size(200, RH) };
            minSizeRow.Controls.Add(_nudMinSize);
            minSizeRow.Controls.Add(new Label { Text = "MB", Location = new Point(84, 5), AutoSize = true });
            AddRow("Min file size:", TIP_MINSIZE, minSizeRow);

            _nudMaxItems     = new NumericUpDown { Width = 80, Minimum = 1, Maximum = 999999, DecimalPlaces = 0 };
            _chkMaxUnlimited = new CheckBox { Text = "Unlimited", AutoSize = true, Location = new Point(84, 4) };
            _nudMaxItems.ValueChanged     += MarkDirty;
            _chkMaxUnlimited.CheckedChanged += (s, e) =>
            {
                _nudMaxItems.Enabled = !_chkMaxUnlimited.Checked;
                MarkDirty(s, e);
            };
            var maxItemsRow = new Panel { Size = new Size(240, RH) };
            maxItemsRow.Controls.AddRange(new Control[] { _nudMaxItems, _chkMaxUnlimited });
            AddRow("Max items:", TIP_MAXITEMS, maxItemsRow);

            // ── Advanced ──────────────────────────────────────────────────
            AddSectionHeader("Advanced");

            const string TIP_MOVE  = "How directories are moved to OK Dir.\n• Auto — use Directory.Move when on the same volume; Robocopy across volumes.\n• Force Robocopy — always use Robocopy (cross-drive, progress visible in log).\n• Force Directory.Move — always use .NET rename (instant on same drive).";
            const string TIP_CACHE = "When checked, the file list is always re-read from disk on every refresh (slower, but always current).";
            const string TIP_DUPE  = "When checked, duplicate detection and similar-name detection are skipped entirely (faster refresh, no coloring).";
            const string TIP_PLOG  = "When checked, per-phase timing lines appear in the log panel after each refresh (useful for performance profiling).";

            _cmbMoveMethod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
            _cmbMoveMethod.Items.AddRange(new object[]
            {
                "Auto (detect by volume)",
                "Force Robocopy",
                "Force Directory.Move",
            });
            _cmbMoveMethod.SelectedIndexChanged += MarkDirty;
            AddRow("Move method:", TIP_MOVE, _cmbMoveMethod);

            _chkNoCache = new CheckBox { Text = "Always read from disk (no cache)", AutoSize = true };
            _chkNoCache.CheckedChanged += MarkDirty;
            AddRow("No cache:", TIP_CACHE, _chkNoCache);

            _chkNoDupeCheck = new CheckBox { Text = "Skip duplicate detection", AutoSize = true };
            _chkNoDupeCheck.CheckedChanged += MarkDirty;
            AddRow("No dupe check:", TIP_DUPE, _chkNoDupeCheck);

            _chkProfileLog = new CheckBox { Text = "Show per-phase timing in log", AutoSize = true };
            _chkProfileLog.CheckedChanged += MarkDirty;
            AddRow("Profile log:", TIP_PLOG, _chkProfileLog);

            panel.Height = y + 8;
            return panel;
        }

        // ════════════════════════════════ LIST ══════════════════════════════

        private void RefreshList(string selectName = null)
        {
            _list.BeginUpdate();
            _list.Items.Clear();

            var profiles = ProfileRegistry.LoadAll();
            var boldFont = new Font(_list.Font, FontStyle.Bold);

            foreach (var p in profiles)
            {
                var item = new ListViewItem(p.Name) { Tag = p };
                item.SubItems.Add(p.Description);

                if (string.Equals(p.Name, _currentProfileName, StringComparison.OrdinalIgnoreCase))
                    item.Font = boldFont;

                _list.Items.Add(item);
            }
            _list.EndUpdate();

            // Restore / choose selection
            ListViewItem toSelect = null;
            if (selectName != null)
                toSelect = _list.Items.Cast<ListViewItem>()
                    .FirstOrDefault(i => string.Equals(i.Text, selectName, StringComparison.OrdinalIgnoreCase));
            toSelect ??= _list.Items.Count > 0 ? _list.Items[0] : null;

            if (toSelect != null)
            {
                toSelect.Selected = true;
                toSelect.EnsureVisible();
            }

            UpdateButtonStates();
        }

        private void UpdateCurrentListItem(Profile p)
        {
            if (_list.SelectedItems.Count == 0) return;
            var item = _list.SelectedItems[0];
            item.Text = p.Name;
            item.SubItems[1].Text = p.Description;
            item.Tag = p;
        }

        private static string SortLabel(int sort) => sort switch
        {
            1 => "Date↓",
            3 => "Name",
            7 => "Rnd",
            _ => sort.ToString(),
        };

        // ════════════════════════════════ EDITOR ════════════════════════════

        private void LoadIntoEditor(Profile p)
        {
            _loading = true;
            try
            {
                _txtName.Text  = p.Name;
                _txtDesc.Text  = p.Description;
                _txtInputDir.Text = p.InputDir;
                _txtOkDir.Text    = p.OkDir;

                _chkInplace.Checked  = p.InplaceBrowsing;
                _txtOkDir.Enabled    = !p.InplaceBrowsing;
                _btnBrowseOk.Enabled = !p.InplaceBrowsing;

                _cmbSort.SelectedIndex = p.SortDir switch { 1 => 0, 3 => 1, _ => 2 };

                _nudMinSize.Value = (decimal)Math.Min(p.MinFileSizeMb, 999999);

                bool unlimited = p.MaxListItems == int.MaxValue;
                _chkMaxUnlimited.Checked = unlimited;
                _nudMaxItems.Enabled     = !unlimited;
                _nudMaxItems.Value       = unlimited ? 1000 : (decimal)Math.Min(p.MaxListItems, 999999);

                _cmbMoveMethod.SelectedIndex = p.MoveWithRobocopy switch
                {
                    "true"  => 1,
                    "false" => 2,
                    _       => 0,
                };

                _chkNoCache.Checked     = p.NoCache;
                _chkNoDupeCheck.Checked = p.NoDupeCheck;
                _chkProfileLog.Checked  = p.ProfileLog;
            }
            finally
            {
                _loading = false;
            }

            _originalName    = p.Name;
            _dirty           = false;
            _btnSave.Enabled = false;
        }

        private void MarkDirty(object sender, EventArgs e)
        {
            if (_loading) return;
            _dirty           = true;
            _btnSave.Enabled = true;
        }

        private Profile ReadFromEditor()
        {
            int    sortDir    = _cmbSort.SelectedIndex switch { 0 => 1, 1 => 3, _ => 7 };
            string moveMethod = _cmbMoveMethod.SelectedIndex switch { 1 => "true", 2 => "false", _ => "" };
            string okDir      = _chkInplace.Checked ? _txtInputDir.Text.Trim() : _txtOkDir.Text.Trim();

            return new Profile
            {
                Name             = _txtName.Text.Trim(),
                Description      = _txtDesc.Text.Trim(),
                InputDir         = _txtInputDir.Text.Trim(),
                OkDir            = okDir,
                SortDir          = sortDir,
                InplaceBrowsing  = _chkInplace.Checked,
                MinFileSizeMb    = (long)_nudMinSize.Value,
                MaxListItems     = _chkMaxUnlimited.Checked ? int.MaxValue : (int)_nudMaxItems.Value,
                MoveWithRobocopy = moveMethod,
                NoCache          = _chkNoCache.Checked,
                NoDupeCheck      = _chkNoDupeCheck.Checked,
                ProfileLog       = _chkProfileLog.Checked,
            };
        }

        private void ChkInplace_CheckedChanged(object sender, EventArgs e)
        {
            bool inplace = _chkInplace.Checked;
            _txtOkDir.Enabled    = !inplace;
            _btnBrowseOk.Enabled = !inplace;
            if (inplace) _txtOkDir.Text = _txtInputDir.Text;
            MarkDirty(sender, e);
        }

        // ════════════════════════════════ EVENTS ════════════════════════════

        private void List_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;

            if (_dirty)
            {
                var res = MessageBox.Show(
                    $"Save changes to \"{_originalName}\"?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Cancel)
                {
                    // Revert list selection — tricky in WinForms; just reload editor
                    // from original to avoid confusion
                    return;
                }
                if (res == DialogResult.Yes) CommitSave();
            }

            var p = _list.SelectedItems[0].Tag as Profile;
            if (p != null) LoadIntoEditor(p);
            UpdateButtonStates();
        }

        private void List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete) { BtnDelete_Click(sender, e); e.Handled = true; }
            if (e.KeyCode == Keys.Enter)  { ActOnSelection();            e.Handled = true; }
        }

        private void List_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_list.HitTest(e.Location).Item != null) ActOnSelection();
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (!ConfirmSaveIfDirty()) return;

            var source = _list.SelectedItems.Count > 0
                ? (_list.SelectedItems[0].Tag as Profile)
                : new Profile { SortDir = 7 };

            var allNames = ProfileRegistry.LoadAll().Select(p => p.Name);
            string newName  = GetUniqueName("New Profile", allNames);
            var    newProf  = source.Clone(newName);
            ProfileRegistry.Save(newProf);
            RefreshList(selectName: newName);
        }

        private void BtnClone_Click(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;
            if (!ConfirmSaveIfDirty()) return;

            var source   = _list.SelectedItems[0].Tag as Profile;
            var allNames = ProfileRegistry.LoadAll().Select(p => p.Name);
            string newName  = GetUniqueName($"Copy of {source.Name}", allNames);
            var    clone    = source.Clone(newName);
            ProfileRegistry.Save(clone);
            RefreshList(selectName: newName);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0) return;
            var p = _list.SelectedItems[0].Tag as Profile;

            if (MessageBox.Show(
                    $"Delete profile \"{p.Name}\"?\n\nThis cannot be undone.",
                    "Delete Profile",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ProfileRegistry.Delete(p.Name);
            _dirty = false;
            RefreshList();
        }

        private void BtnSave_Click(object sender, EventArgs e) => CommitSave();

        private void BtnOpenProfile_Click(object sender, EventArgs e)
        {
            if (_list.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select a profile first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_dirty && !ConfirmSaveIfDirty()) return;

            SelectedProfile = _list.SelectedItems[0].Tag as Profile;
            DialogResult    = DialogResult.OK;
            Close();
        }

        // ════════════════════════════════ SAVE ══════════════════════════════

        private void CommitSave()
        {
            var p = ReadFromEditor();

            if (string.IsNullOrWhiteSpace(p.Name))
            {
                MessageBox.Show("Profile name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check for name collision (different profile, same name)
            bool renamed = !string.Equals(_originalName, p.Name, StringComparison.OrdinalIgnoreCase);
            if (renamed && ProfileRegistry.Load(p.Name) != null)
            {
                MessageBox.Show($"A profile named \"{p.Name}\" already exists.\nChoose a different name.",
                    "Name Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ProfileRegistry.Save(p, oldName: renamed ? _originalName : null);
            UpdateCurrentListItem(p);
            _originalName    = p.Name;
            _dirty           = false;
            _btnSave.Enabled = false;
        }

        // ════════════════════════════════ LAUNCH ════════════════════════════

        private void LaunchSelected(bool exitCurrent)
        {
            if (_list.SelectedItems.Count == 0) return;
            if (_dirty && !ConfirmSaveIfDirty()) return;

            var p = _list.SelectedItems[0].Tag as Profile;

            // Validate directories
            var missing = new List<string>();
            if (!Directory.Exists(p.InputDir)) missing.Add($"  Input Dir:  {p.InputDir}");
            if (!p.InplaceBrowsing && !string.Equals(p.InputDir, p.OkDir, StringComparison.OrdinalIgnoreCase)
                && !Directory.Exists(p.OkDir))
                missing.Add($"  OK Dir:     {p.OkDir}");

            if (missing.Count > 0 &&
                MessageBox.Show(
                    $"One or more directories do not exist:\n\n{string.Join("\n", missing)}\n\nLaunch anyway?",
                    "Directory Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Prevent launching a profile that's already running — focus its window instead
            if (System.Threading.Mutex.TryOpenExisting(ProfileRegistry.MutexName(p.Name), out var existing))
            {
                existing.Close();
                Util.FocusProfileWindow(p.Name);
                return;
            }

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName        = Application.ExecutablePath,
                    Arguments       = $"\"{p.Name}\"",
                    UseShellExecute = false,
                }
            };
            proc.Start();

            if (exitCurrent)
            {
                // Poll until the new instance shows its main window (up to 15 s)
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (proc.MainWindowHandle == IntPtr.Zero && DateTime.UtcNow < deadline)
                {
                    System.Threading.Thread.Sleep(200);
                    proc.Refresh();
                    Application.DoEvents();
                }
                Application.Exit();
            }
        }

        private void ActOnSelection()
        {
            if (_startupMode) BtnOpenProfile_Click(this, EventArgs.Empty);
            else LaunchSelected(exitCurrent: false);
        }

        // ════════════════════════════════ HELPERS ═══════════════════════════

        private bool ConfirmSaveIfDirty()
        {
            if (!_dirty) return true;
            var res = MessageBox.Show(
                $"Save changes to \"{_originalName}\"?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return false;
            if (res == DialogResult.Yes) CommitSave();
            return true;
        }

        private void UpdateButtonStates()
        {
            bool has = _list.SelectedItems.Count > 0;
            _btnClone.Enabled  = has;
            _btnDelete.Enabled = has;
            if (_btnLaunch      != null) _btnLaunch.Enabled      = has;
            if (_btnLaunchExit  != null) _btnLaunchExit.Enabled  = has;
            if (_btnOpenProfile != null) _btnOpenProfile.Enabled = has;
        }

        private void BrowseFolder(TextBox target)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description  = "Select folder",
                SelectedPath = Directory.Exists(target.Text) ? target.Text : "",
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
                target.Text = dlg.SelectedPath;
        }

        // ── Factory helpers ─────────────────────────────────────────────────

        private static Button MakeButton(string text, int width, Point location) => new Button
        {
            Text     = text,
            Size     = new Size(width, 28),
            Location = location,
        };

        private static Label MakeHeader(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Top,
            Height    = 28,
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(6, 0, 0, 0),
            BackColor = SystemColors.ControlLight,
        };

        private TextBox MakeDirTextBox() => new TextBox
        {
            Width              = 360,
            AutoCompleteMode   = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.FileSystemDirectories,
        };

        private Button MakeBrowseButton(TextBox target)
        {
            var btn = new Button { Text = "…", Size = new Size(26, 23) };
            btn.Click += (s, e) => BrowseFolder(target);
            return btn;
        }

        private static Panel MakeDirRow(TextBox txt, Button browse)
        {
            var row = new Panel { Size = new Size(390, 24) };
            txt.Width        = 358;
            browse.Location  = new Point(362, 0);
            browse.Size      = new Size(26, txt.Height);
            row.Controls.AddRange(new Control[] { txt, browse });
            return row;
        }

        private static string GetUniqueName(string baseName, IEnumerable<string> existing)
        {
            var taken = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            if (!taken.Contains(baseName)) return baseName;
            for (int i = 2; i < 10000; i++)
            {
                string candidate = $"{baseName} ({i})";
                if (!taken.Contains(candidate)) return candidate;
            }
            return $"{baseName} {Guid.NewGuid():N}";
        }
    }
}
