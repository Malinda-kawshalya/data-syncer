using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public class FormMain : Form
    {
        private MenuStrip menuStrip;
        private DataGridView dgvJobs;
        private Button btnAddJob, btnStartStop;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;

        public FormMain()
        {
            Text = "DataSyncer - Main Dashboard";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeControls();
        }

        private void InitializeControls()
        {
            // Menu
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var settingsMenu = new ToolStripMenuItem("Settings");
            var helpMenu = new ToolStripMenuItem("Help");

            settingsMenu.DropDownItems.Add("Connection Settings", null, (s, e) => new FormConnection().ShowDialog());
            settingsMenu.DropDownItems.Add("Schedule Settings", null, (s, e) => new FormSchedule().ShowDialog());
            settingsMenu.DropDownItems.Add("Filters", null, (s, e) => new FormFilters().ShowDialog());

            fileMenu.DropDownItems.Add("Logs", null, (s, e) => new FormLogs().ShowDialog());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

            menuStrip.Items.AddRange(new[] { fileMenu, settingsMenu, helpMenu });
            Controls.Add(menuStrip);

            // Job Table
            dgvJobs = new DataGridView()
            {
                Location = new Point(10, 40),
                Size = new Size(860, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };
            dgvJobs.Columns.Add("JobName", "Job Name");
            dgvJobs.Columns.Add("Schedule", "Schedule");
            dgvJobs.Columns.Add("Status", "Status");
            Controls.Add(dgvJobs);

            // Buttons
            btnAddJob = new Button() { Text = "Add New Job", Location = new Point(10, 460), Size = new Size(120, 30) };
            btnStartStop = new Button() { Text = "Start Service", Location = new Point(140, 460), Size = new Size(120, 30) };

            btnAddJob.Click += (s, e) => MessageBox.Show("Add Job Clicked");
            btnStartStop.Click += (s, e) => MessageBox.Show("Start/Stop Clicked");

            Controls.Add(btnAddJob);
            Controls.Add(btnStartStop);

            // Status
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Disconnected");
            statusStrip.Items.Add(lblStatus);
            Controls.Add(statusStrip);
        }
    }
}
