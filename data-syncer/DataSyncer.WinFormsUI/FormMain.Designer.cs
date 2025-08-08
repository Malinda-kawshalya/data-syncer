namespace DataSyncer.WinFormsUI
{
    partial class FormMain
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
            dgvJobs = new DataGridView();
            JobName = new DataGridViewTextBoxColumn();
            LocalPath = new DataGridViewTextBoxColumn();
            RemotePath = new DataGridViewTextBoxColumn();
            Direction = new DataGridViewTextBoxColumn();
            NextRun = new DataGridViewTextBoxColumn();
            Status = new DataGridViewTextBoxColumn();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            connectionToolStripMenuItem = new ToolStripMenuItem();
            scheduleToolStripMenuItem = new ToolStripMenuItem();
            filtersToolStripMenuItem = new ToolStripMenuItem();
            logsToolStripMenuItem = new ToolStripMenuItem();
            menuStrip3 = new MenuStrip();
            btnAddJob = new Button();
            btnEditJob = new Button();
            btnRemoveJob = new Button();
            btnStartService = new Button();
            btnStopService = new Button();
            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)dgvJobs).BeginInit();
            menuStrip3.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvJobs
            // 
            dgvJobs.AllowUserToOrderColumns = true;
            dgvJobs.BackgroundColor = SystemColors.ControlLight;
            dgvJobs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvJobs.Columns.AddRange(new DataGridViewColumn[] { JobName, LocalPath, RemotePath, Direction, NextRun, Status });
            dgvJobs.Location = new Point(0, 27);
            dgvJobs.Name = "dgvJobs";
            dgvJobs.Size = new Size(642, 381);
            dgvJobs.TabIndex = 3;
            dgvJobs.CellContentClick += dgvJobs_CellContentClick;
            // 
            // JobName
            // 
            JobName.HeaderText = "JobName";
            JobName.Name = "JobName";
            // 
            // LocalPath
            // 
            LocalPath.HeaderText = "LocalPath";
            LocalPath.Name = "LocalPath";
            // 
            // RemotePath
            // 
            RemotePath.HeaderText = "RemotePath";
            RemotePath.Name = "RemotePath";
            // 
            // Direction
            // 
            Direction.HeaderText = "Direction";
            Direction.Name = "Direction";
            // 
            // NextRun
            // 
            NextRun.HeaderText = "NextRun";
            NextRun.Name = "NextRun";
            // 
            // Status
            // 
            Status.HeaderText = "Status";
            Status.Name = "Status";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { connectionToolStripMenuItem, scheduleToolStripMenuItem, filtersToolStripMenuItem, logsToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(63, 20);
            settingsToolStripMenuItem.Text = "settings ";
            // 
            // connectionToolStripMenuItem
            // 
            connectionToolStripMenuItem.Name = "connectionToolStripMenuItem";
            connectionToolStripMenuItem.Size = new Size(180, 22);
            connectionToolStripMenuItem.Text = "Connection";
            // 
            // scheduleToolStripMenuItem
            // 
            scheduleToolStripMenuItem.Name = "scheduleToolStripMenuItem";
            scheduleToolStripMenuItem.Size = new Size(180, 22);
            scheduleToolStripMenuItem.Text = "Schedule";
            // 
            // filtersToolStripMenuItem
            // 
            filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
            filtersToolStripMenuItem.Size = new Size(180, 22);
            filtersToolStripMenuItem.Text = "Filters";
            // 
            // logsToolStripMenuItem
            // 
            logsToolStripMenuItem.Name = "logsToolStripMenuItem";
            logsToolStripMenuItem.Size = new Size(180, 22);
            logsToolStripMenuItem.Text = "Logs";
            // 
            // menuStrip3
            // 
            menuStrip3.Items.AddRange(new ToolStripItem[] { settingsToolStripMenuItem });
            menuStrip3.Location = new Point(0, 0);
            menuStrip3.Name = "menuStrip3";
            menuStrip3.Size = new Size(642, 24);
            menuStrip3.TabIndex = 2;
            menuStrip3.Text = "menuStrip3";
            // 
            // btnAddJob
            // 
            btnAddJob.Location = new Point(57, 337);
            btnAddJob.Name = "btnAddJob";
            btnAddJob.Size = new Size(75, 23);
            btnAddJob.TabIndex = 4;
            btnAddJob.Text = "Add Job";
            btnAddJob.UseVisualStyleBackColor = true;
            // 
            // btnEditJob
            // 
            btnEditJob.Location = new Point(162, 337);
            btnEditJob.Name = "btnEditJob";
            btnEditJob.Size = new Size(75, 23);
            btnEditJob.TabIndex = 5;
            btnEditJob.Text = "Edit Job";
            btnEditJob.UseVisualStyleBackColor = true;
            // 
            // btnRemoveJob
            // 
            btnRemoveJob.Location = new Point(271, 337);
            btnRemoveJob.Name = "btnRemoveJob";
            btnRemoveJob.Size = new Size(75, 23);
            btnRemoveJob.TabIndex = 6;
            btnRemoveJob.Text = "Remove Job";
            btnRemoveJob.UseVisualStyleBackColor = true;
            // 
            // btnStartService
            // 
            btnStartService.Location = new Point(390, 337);
            btnStartService.Name = "btnStartService";
            btnStartService.Size = new Size(75, 23);
            btnStartService.TabIndex = 7;
            btnStartService.Text = "Start Service";
            btnStartService.UseVisualStyleBackColor = true;
            // 
            // btnStopService
            // 
            btnStopService.Location = new Point(507, 337);
            btnStopService.Name = "btnStopService";
            btnStopService.Size = new Size(75, 23);
            btnStopService.TabIndex = 8;
            btnStopService.Text = "Stop Service";
            btnStopService.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip1.Location = new Point(0, 428);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(642, 22);
            statusStrip1.TabIndex = 9;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(90, 17);
            lblStatus.Text = "Service stopped";
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(642, 450);
            Controls.Add(statusStrip1);
            Controls.Add(btnStopService);
            Controls.Add(btnStartService);
            Controls.Add(btnRemoveJob);
            Controls.Add(btnEditJob);
            Controls.Add(btnAddJob);
            Controls.Add(dgvJobs);
            Controls.Add(menuStrip3);
            Name = "FormMain";
            Text = "FormMain";
            Load += FormMain_Load;
            ((System.ComponentModel.ISupportInitialize)dgvJobs).EndInit();
            menuStrip3.ResumeLayout(false);
            menuStrip3.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DataGridView dgvJobs;
        private DataGridViewTextBoxColumn JobName;
        private DataGridViewTextBoxColumn LocalPath;
        private DataGridViewTextBoxColumn RemotePath;
        private DataGridViewTextBoxColumn Direction;
        private DataGridViewTextBoxColumn NextRun;
        private DataGridViewTextBoxColumn Status;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem connectionToolStripMenuItem;
        private ToolStripMenuItem scheduleToolStripMenuItem;
        private ToolStripMenuItem filtersToolStripMenuItem;
        private ToolStripMenuItem logsToolStripMenuItem;
        private MenuStrip menuStrip3;
        private Button btnAddJob;
        private Button btnEditJob;
        private Button btnRemoveJob;
        private Button btnStartService;
        private Button btnStopService;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatus;
    }
}