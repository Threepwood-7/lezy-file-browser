
namespace LezyFileBrowser
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lstFiles = new System.Windows.Forms.ListView();
            this.colDirName = new System.Windows.Forms.ColumnHeader();
            this.colDate = new System.Windows.Forms.ColumnHeader();
            this.colSize = new System.Windows.Forms.ColumnHeader();
            this.colWod = new System.Windows.Forms.ColumnHeader();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.grpBottom = new System.Windows.Forms.GroupBox();
            this.btnLaunchItem = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.ckAutoplay = new System.Windows.Forms.CheckBox();
            this.ckShowDupesOnly = new System.Windows.Forms.CheckBox();
            this.btnRename = new System.Windows.Forms.Button();
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.btnKillMp = new System.Windows.Forms.Button();
            this.txtSelectedDir = new System.Windows.Forms.TextBox();
            this.btnDeleteDir = new System.Windows.Forms.Button();
            this.txtOkDir = new System.Windows.Forms.TextBox();
            this.txtInputDir = new System.Windows.Forms.TextBox();
            this.ckForceFullScreen = new System.Windows.Forms.CheckBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsSpring = new System.Windows.Forms.ToolStripStatusLabel();
            this.prgLoading = new System.Windows.Forms.ToolStripProgressBar();
            this.timUIUpdate = new System.Windows.Forms.Timer(this.components);
            this.lblFileName = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.grpBottom.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer1
            //
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 28);
            this.splitContainer1.Name = "splitContainer1";
            //
            // splitContainer1.Panel1
            //
            this.splitContainer1.Panel1.Controls.Add(this.lstFiles);
            //
            // splitContainer1.Panel2
            //
            this.splitContainer1.Panel2.Controls.Add(this.txtLog);
            this.splitContainer1.Panel2.Controls.Add(this.grpBottom);
            this.splitContainer1.Size = new System.Drawing.Size(1355, 810);
            this.splitContainer1.SplitterDistance = 693;
            this.splitContainer1.TabIndex = 0;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            //
            // lstFiles
            //
            this.lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstFiles.BackColor = System.Drawing.SystemColors.ControlDark;
            this.lstFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDirName,
            this.colDate,
            this.colSize,
            this.colWod});
            this.lstFiles.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
            this.lstFiles.FullRowSelect = true;
            this.lstFiles.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lstFiles.HideSelection = false;
            this.lstFiles.Location = new System.Drawing.Point(0, 0);
            this.lstFiles.MultiSelect = false;
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(690, 753);
            this.lstFiles.TabIndex = 0;
            this.lstFiles.UseCompatibleStateImageBehavior = false;
            this.lstFiles.View = System.Windows.Forms.View.Details;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.lstDirectories_SelectedIndexChanged);
            this.lstFiles.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lstDirectories_KeyDown);
            this.lstFiles.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lstFiles_MouseClick);
            this.lstFiles.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lstFiles_MouseDoubleClick);
            this.lstFiles.Resize += new System.EventHandler(this.lstFiles_Resize);
            //
            // colDirName
            //
            this.colDirName.Text = "Dir";
            this.colDirName.Width = 800;
            //
            // colDate
            //
            this.colDate.Text = "Date";
            this.colDate.Width = 140;
            //
            // colSize
            //
            this.colSize.Text = "Size";
            this.colSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colSize.Width = 120;
            //
            // colWod
            //
            this.colWod.Text = "WoD";
            this.colWod.Width = 40;
            //
            // txtLog
            //
            this.txtLog.BackColor = System.Drawing.SystemColors.ControlDark;
            this.txtLog.DetectUrls = false;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(0, 0);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(658, 551);
            this.txtLog.TabIndex = 0;
            this.txtLog.TabStop = false;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            //
            // grpBottom
            //
            this.grpBottom.Controls.Add(this.btnLaunchItem);
            this.grpBottom.Controls.Add(this.btnRefresh);
            this.grpBottom.Controls.Add(this.ckShowDupesOnly);
            this.grpBottom.Controls.Add(this.ckAutoplay);
            this.grpBottom.Controls.Add(this.btnRename);
            this.grpBottom.Controls.Add(this.txtFileName);
            this.grpBottom.Controls.Add(this.btnKillMp);
            this.grpBottom.Controls.Add(this.txtSelectedDir);
            this.grpBottom.Controls.Add(this.btnDeleteDir);
            this.grpBottom.Controls.Add(this.txtOkDir);
            this.grpBottom.Controls.Add(this.txtInputDir);
            this.grpBottom.Controls.Add(this.ckForceFullScreen);
            this.grpBottom.Controls.Add(this.btnExit);
            this.grpBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.grpBottom.Location = new System.Drawing.Point(0, 551);
            this.grpBottom.Name = "grpBottom";
            this.grpBottom.Size = new System.Drawing.Size(658, 259);
            this.grpBottom.TabIndex = 1;
            this.grpBottom.TabStop = false;
            //
            // btnLaunchItem
            //
            this.btnLaunchItem.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLaunchItem.Location = new System.Drawing.Point(166, 211);
            this.btnLaunchItem.Name = "btnLaunchItem";
            this.btnLaunchItem.Size = new System.Drawing.Size(75, 23);
            this.btnLaunchItem.TabIndex = 11;
            this.btnLaunchItem.Text = "La&unch";
            this.btnLaunchItem.UseVisualStyleBackColor = true;
            this.btnLaunchItem.Click += new System.EventHandler(this.btnLaunchItem_Click);
            //
            // btnRefresh
            //
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.Location = new System.Drawing.Point(247, 211);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 10;
            this.btnRefresh.Text = "Refres&h";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            //
            // ckAutoplay
            //
            this.ckAutoplay.AutoSize = true;
            this.ckAutoplay.Checked = true;
            this.ckAutoplay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ckAutoplay.Location = new System.Drawing.Point(121, 19);
            this.ckAutoplay.Name = "ckAutoplay";
            this.ckAutoplay.Size = new System.Drawing.Size(67, 17);
            this.ckAutoplay.TabIndex = 9;
            this.ckAutoplay.Text = "Autopla&y";
            this.ckAutoplay.UseVisualStyleBackColor = true;
            //
            // ckShowDupesOnly
            //
            this.ckShowDupesOnly.AutoSize = true;
            this.ckShowDupesOnly.Location = new System.Drawing.Point(200, 19);
            this.ckShowDupesOnly.Name = "ckShowDupesOnly";
            this.ckShowDupesOnly.Size = new System.Drawing.Size(100, 17);
            this.ckShowDupesOnly.TabIndex = 13;
            this.ckShowDupesOnly.Text = "Show &dupez only";
            this.ckShowDupesOnly.UseVisualStyleBackColor = true;
            this.ckShowDupesOnly.CheckedChanged += new System.EventHandler(this.ckShowDupesOnly_CheckedChanged);
            //
            // btnRename
            //
            this.btnRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRename.Location = new System.Drawing.Point(328, 211);
            this.btnRename.Name = "btnRename";
            this.btnRename.Size = new System.Drawing.Size(75, 23);
            this.btnRename.TabIndex = 8;
            this.btnRename.Text = "Re&name";
            this.btnRename.UseVisualStyleBackColor = true;
            this.btnRename.Click += new System.EventHandler(this.btnRename_Click);
            //
            // txtFileName
            //
            this.txtFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileName.Location = new System.Drawing.Point(7, 121);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(639, 20);
            this.txtFileName.TabIndex = 7;
            this.txtFileName.TabStop = false;
            //
            // btnKillMp
            //
            this.btnKillMp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKillMp.Location = new System.Drawing.Point(409, 211);
            this.btnKillMp.Name = "btnKillMp";
            this.btnKillMp.Size = new System.Drawing.Size(75, 23);
            this.btnKillMp.TabIndex = 6;
            this.btnKillMp.Text = "&Kill MP";
            this.btnKillMp.UseVisualStyleBackColor = true;
            this.btnKillMp.Click += new System.EventHandler(this.btnKillMp_Click);
            //
            // txtSelectedDir
            //
            this.txtSelectedDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSelectedDir.Location = new System.Drawing.Point(7, 95);
            this.txtSelectedDir.Name = "txtSelectedDir";
            this.txtSelectedDir.ReadOnly = true;
            this.txtSelectedDir.Size = new System.Drawing.Size(639, 20);
            this.txtSelectedDir.TabIndex = 5;
            this.txtSelectedDir.TabStop = false;
            //
            // btnDeleteDir
            //
            this.btnDeleteDir.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDeleteDir.Location = new System.Drawing.Point(490, 211);
            this.btnDeleteDir.Name = "btnDeleteDir";
            this.btnDeleteDir.Size = new System.Drawing.Size(75, 23);
            this.btnDeleteDir.TabIndex = 4;
            this.btnDeleteDir.Text = "Delete Di&r";
            this.btnDeleteDir.UseVisualStyleBackColor = true;
            this.btnDeleteDir.Click += new System.EventHandler(this.btnDeleteDir_Click);
            //
            // txtOkDir
            //
            this.txtOkDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOkDir.Location = new System.Drawing.Point(7, 69);
            this.txtOkDir.Name = "txtOkDir";
            this.txtOkDir.ReadOnly = true;
            this.txtOkDir.Size = new System.Drawing.Size(639, 20);
            this.txtOkDir.TabIndex = 3;
            this.txtOkDir.TabStop = false;
            //
            // txtInputDir
            //
            this.txtInputDir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInputDir.Location = new System.Drawing.Point(7, 43);
            this.txtInputDir.Name = "txtInputDir";
            this.txtInputDir.ReadOnly = true;
            this.txtInputDir.Size = new System.Drawing.Size(639, 20);
            this.txtInputDir.TabIndex = 2;
            this.txtInputDir.TabStop = false;
            //
            // ckForceFullScreen
            //
            this.ckForceFullScreen.AutoSize = true;
            this.ckForceFullScreen.Location = new System.Drawing.Point(6, 19);
            this.ckForceFullScreen.Name = "ckForceFullScreen";
            this.ckForceFullScreen.Size = new System.Drawing.Size(109, 17);
            this.ckForceFullScreen.TabIndex = 1;
            this.ckForceFullScreen.Text = "Force Ful&l Screen";
            this.ckForceFullScreen.UseVisualStyleBackColor = true;
            //
            // btnExit
            //
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Location = new System.Drawing.Point(571, 211);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(75, 23);
            this.btnExit.TabIndex = 0;
            this.btnExit.Text = "E&xit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            //
            // tsSpring
            //
            this.tsSpring.Name = "tsSpring";
            this.tsSpring.Spring = true;
            this.tsSpring.Text = "";
            //
            // prgLoading
            //
            this.prgLoading.MarqueeAnimationSpeed = 25;
            this.prgLoading.Name = "prgLoading";
            this.prgLoading.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.prgLoading.Width = 200;
            this.prgLoading.Visible = false;
            //
            // statusStrip1
            //
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { this.tsSpring, this.prgLoading });
            this.statusStrip1.Location = new System.Drawing.Point(0, 816);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1355, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            //
            // timUIUpdate
            //
            this.timUIUpdate.Enabled = true;
            this.timUIUpdate.Interval = 300;
            this.timUIUpdate.Tick += new System.EventHandler(this.timUIUpdate_Tick);
            //
            // lblFileName
            //
            this.lblFileName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.lblFileName.Location = new System.Drawing.Point(0, 0);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(1355, 28);
            this.lblFileName.TabIndex = 12;
            this.lblFileName.Text = "";
            this.lblFileName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1355, 838);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.lblFileName);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "-";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Activated += new System.EventHandler(this.MainForm_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.grpBottom.ResumeLayout(false);
            this.grpBottom.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView lstFiles;
        private System.Windows.Forms.ColumnHeader colDirName;
        private System.Windows.Forms.ColumnHeader colDate;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Timer timUIUpdate;
        private System.Windows.Forms.GroupBox grpBottom;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.ColumnHeader colWod;
        private System.Windows.Forms.CheckBox ckForceFullScreen;
        private System.Windows.Forms.TextBox txtInputDir;
        private System.Windows.Forms.TextBox txtOkDir;
        private System.Windows.Forms.TextBox txtSelectedDir;
        private System.Windows.Forms.Button btnDeleteDir;
        private System.Windows.Forms.Button btnKillMp;
        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.CheckBox ckAutoplay;
        private System.Windows.Forms.CheckBox ckShowDupesOnly;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnLaunchItem;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.ToolStripStatusLabel tsSpring;
        private System.Windows.Forms.ToolStripProgressBar prgLoading;
    }
}
