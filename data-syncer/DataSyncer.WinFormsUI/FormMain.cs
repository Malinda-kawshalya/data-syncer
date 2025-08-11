using System;
using System.Drawing;
using System.Windows.Forms;
using DataSyncer.Core.Services;
using DataSyncer.WinFormsUI.Utilities;
using System.Threading.Tasks;

namespace DataSyncer.WinFormsUI
{
    public class FormMain : Form
    {
        private MenuStrip menuStrip = null!;
        private DataGridView dgvJobs = null!;
        private Button btnAddJob = null!, btnStartStop = null!, btnRefresh = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel lblStatus = null!;
        private System.Windows.Forms.Timer _statusTimer = null!;

        public FormMain()
        {
            Text = "DataSyncer - Main Dashboard";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            
            InitializeControls();
            InitializeStatusTimer();
            _ = CheckServiceConnection(); // Fire and forget
        }

        private void InitializeControls()
        {
            // Menu
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var settingsMenu = new ToolStripMenuItem("Settings");
            var helpMenu = new ToolStripMenuItem("Help");

            settingsMenu.DropDownItems.Add("Connection Settings", null, (s, e) => OpenConnectionSettings());
            settingsMenu.DropDownItems.Add("Schedule Settings", null, (s, e) => new FormSchedule().ShowDialog());
            settingsMenu.DropDownItems.Add("Filters", null, (s, e) => new FormFilters().ShowDialog());

            fileMenu.DropDownItems.Add("Logs", null, (s, e) => new FormLogs().ShowDialog());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());

            helpMenu.DropDownItems.Add("About", null, (s, e) => ShowAbout());

            menuStrip.Items.AddRange(new[] { fileMenu, settingsMenu, helpMenu });
            Controls.Add(menuStrip);

            // Job Table
            dgvJobs = new DataGridView()
            {
                Location = new Point(10, 40),
                Size = new Size(860, 400),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            
            dgvJobs.Columns.Add("JobName", "Job Name");
            dgvJobs.Columns.Add("Source", "Source");
            dgvJobs.Columns.Add("Destination", "Destination");
            dgvJobs.Columns.Add("Schedule", "Schedule");
            dgvJobs.Columns.Add("Status", "Status");
            dgvJobs.Columns.Add("LastRun", "Last Run");
            
            Controls.Add(dgvJobs);

            // Buttons
            btnAddJob = new Button() { Text = "Add New Job", Location = new Point(10, 460), Size = new Size(120, 35) };
            btnStartStop = new Button() { Text = "Start Service", Location = new Point(140, 460), Size = new Size(120, 35) };
            btnRefresh = new Button() { Text = "Refresh", Location = new Point(270, 460), Size = new Size(80, 35) };

            btnAddJob.Click += BtnAddJob_Click;
            btnStartStop.Click += BtnStartStop_Click;
            btnRefresh.Click += BtnRefresh_Click;

            Controls.Add(btnAddJob);
            Controls.Add(btnStartStop);
            Controls.Add(btnRefresh);

            // Status
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Checking service connection...");
            statusStrip.Items.Add(lblStatus);
            Controls.Add(statusStrip);
        }

        private void InitializeStatusTimer()
        {
            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 10000; // Check every 10 seconds
            _statusTimer.Tick += async (s, e) => await CheckServiceConnection();
            _statusTimer.Start();
        }

        private async void BtnAddJob_Click(object? sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("=== Add Job button clicked ===");
                
                // First check in-memory settings (most up-to-date)
                var settings = ConnectionManager.Instance.CurrentSettings;
                
                // If no in-memory settings, try loading from file
                if (settings == null)
                {
                    settings = ConfigurationManager.LoadConnectionSettings();
                }
                
                if (settings == null)
                {
                    Console.WriteLine("=== No connection settings found ===");
                    var result = MessageBox.Show("No connection settings found. Would you like to configure them now?", 
                        "Configuration Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        OpenConnectionSettings();
                    }
                    return;
                }

                if (!settings.IsValid())
                {
                    Console.WriteLine("=== Connection settings are invalid ===");
                    var result = MessageBox.Show("Connection settings are invalid. Would you like to configure them now?", 
                        "Configuration Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        OpenConnectionSettings();
                    }
                    return;
                }

                Console.WriteLine($"=== Sending START_TRANSFER command with settings: {settings.Host}:{settings.Port} ===");

                // Send command to service to start a transfer job using ConnectionManager
                bool success = await ConnectionManager.Instance.StartTransferAsync(settings);
                
                if (success)
                {
                    lblStatus.Text = "Transfer job started";
                    RefreshJobs();
                    MessageBox.Show("Transfer started successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to start transfer. Make sure the service is running.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting transfer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnStartStop_Click(object? sender, EventArgs e)
        {
            try
            {
                bool isConnected = await ConnectionManager.Instance.TestServiceConnectionAsync();
                
                if (isConnected)
                {
                    // Service is running, try to stop it
                    var pipeClient = new NamedPipeClient();
                    await pipeClient.SendCommandAsync<object>("STOP_SERVICE");
                    lblStatus.Text = "Stop command sent to service";
                }
                else
                {
                    MessageBox.Show("Service is not running. Please start the DataSyncer Windows Service first.", 
                        "Service Not Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error communicating with service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            RefreshJobs();
        }

        private void OpenConnectionSettings()
        {
            using (var form = new FormConnection())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    lblStatus.Text = "Connection settings updated";
                }
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show("DataSyncer v1.0\nA simple file synchronization tool\n\nFeatures:\n- FTP/SFTP support\n- Scheduled transfers\n- File filtering", 
                "About DataSyncer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task CheckServiceConnection()
        {
            try
            {
                bool isConnected = await ConnectionManager.Instance.TestServiceConnectionAsync();
                
                if (isConnected)
                {
                    lblStatus.Text = "Connected to service";
                    lblStatus.ForeColor = Color.Green;
                    btnStartStop.Text = "Stop Service";
                    btnAddJob.Enabled = true;
                }
                else
                {
                    lblStatus.Text = "Service not running";
                    lblStatus.ForeColor = Color.Red;
                    btnStartStop.Text = "Start Service";
                    btnAddJob.Enabled = false;
                }
            }
            catch
            {
                lblStatus.Text = "Connection error";
                lblStatus.ForeColor = Color.Red;
                btnStartStop.Text = "Start Service";
                btnAddJob.Enabled = false;
            }
        }

        private void RefreshJobs()
        {
            // Clear existing rows
            dgvJobs.Rows.Clear();
            
            try
            {
                var settings = ConnectionManager.Instance.CurrentSettings ?? ConfigurationManager.LoadConnectionSettings();
                if (settings != null && settings.IsValid())
                {
                    // Add a sample job based on current settings
                    dgvJobs.Rows.Add(
                        "Main Transfer Job",
                        settings.SourcePath,
                        settings.DestinationPath,
                        "Manual",
                        "Ready",
                        "Never"
                    );
                }
                else
                {
                    dgvJobs.Rows.Add(
                        "No Configuration",
                        "N/A",
                        "N/A",
                        "N/A",
                        "Not Configured",
                        "N/A"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing jobs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
