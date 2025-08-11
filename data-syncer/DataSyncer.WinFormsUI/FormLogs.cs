using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.WinFormsUI.Utilities;

namespace DataSyncer.WinFormsUI
{
    public class FormLogs : Form
    {
        private DataGridView dgvLogs = null!;
        private TextBox txtSearch = null!;
        private Button btnClear = null!, btnRefresh = null!, btnExport = null!;
        private DateTimePicker dtpDate = null!;
        private CheckBox chkAllDates = null!;
        private List<TransferLog> _allLogs = new List<TransferLog>();
        private readonly ConnectionManager _connectionManager;

        public FormLogs()
        {
            Text = "Transfer Logs";
            Size = new Size(800, 500);
            StartPosition = FormStartPosition.CenterParent;
            
            // Get the singleton instance of ConnectionManager
            _connectionManager = ConnectionManager.Instance;

            InitializeControls();
            LoadLogsAsync().ConfigureAwait(false);
        }

        private void InitializeControls()
        {
            // Search controls
            Label lblSearch = new Label() { Text = "Search:", Location = new Point(10, 15), Size = new Size(60, 20) };
            txtSearch = new TextBox() { Location = new Point(70, 12), Width = 200 };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            // Date picker
            Label lblDate = new Label() { Text = "Date:", Location = new Point(290, 15), Size = new Size(40, 20) };
            dtpDate = new DateTimePicker() { 
                Location = new Point(330, 12), 
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpDate.ValueChanged += DtpDate_ValueChanged;
            
            // All dates checkbox
            chkAllDates = new CheckBox() {
                Text = "All Dates",
                Location = new Point(460, 12),
                AutoSize = true,
                Checked = true
            };
            chkAllDates.CheckedChanged += ChkAllDates_CheckedChanged;

            // Logs grid
            dgvLogs = new DataGridView()
            {
                Location = new Point(10, 50),
                Size = new Size(760, 350),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            dgvLogs.Columns.Add("Time", "Time");
            dgvLogs.Columns.Add("File", "File");
            dgvLogs.Columns.Add("Status", "Status");
            dgvLogs.Columns.Add("Duration", "Duration (ms)");
            dgvLogs.Columns.Add("Size", "Size (bytes)");
            dgvLogs.Columns.Add("Protocol", "Protocol");
            dgvLogs.Columns.Add("Message", "Message");
            
            // Set column widths
            dgvLogs.Columns[0].Width = 120; // Time
            dgvLogs.Columns[1].Width = 150; // File
            dgvLogs.Columns[2].Width = 80;  // Status
            dgvLogs.Columns[3].Width = 80;  // Duration
            dgvLogs.Columns[4].Width = 80;  // Size
            dgvLogs.Columns[5].Width = 80;  // Protocol
            dgvLogs.Columns[6].Width = 170; // Message

            // Buttons
            btnRefresh = new Button() { Text = "Refresh", Location = new Point(10, 420), Size = new Size(80, 30) };
            btnRefresh.Click += BtnRefresh_Click;
            
            btnClear = new Button() { Text = "Clear Logs", Location = new Point(100, 420), Size = new Size(80, 30) };
            btnClear.Click += BtnClear_Click;
            
            btnExport = new Button() { Text = "Export", Location = new Point(190, 420), Size = new Size(80, 30) };
            btnExport.Click += BtnExport_Click;

            Controls.AddRange(new Control[] { 
                lblSearch, txtSearch, 
                lblDate, dtpDate, chkAllDates,
                dgvLogs, 
                btnRefresh, btnClear, btnExport 
            });
        }
        
        private async Task LoadLogsAsync()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                
                // Check if service is available
                bool serviceConnected = await _connectionManager.IsServiceConnectedAsync();
                
                if (serviceConnected)
                {
                    // Get logs from service
                    await LoadLogsFromServiceAsync();
                }
                else
                {
                    // Load from local storage if service not available
                    LoadLogsFromStorage();
                }
                
                // Apply any search filter
                DisplayFilteredLogs();
                
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Cursor = Cursors.Default;
            }
        }
        
        private async Task LoadLogsFromServiceAsync()
        {
            try
            {
                // Prepare filter data
                var filterData = new
                {
                    Filter = txtSearch.Text,
                    MaxResults = 1000,
                    Date = chkAllDates.Checked ? (DateTime?)null : dtpDate.Value.Date
                };
                
                // Send request to service
                var response = await _connectionManager.SendMessageAsync(
                    JsonSerializer.Serialize(new { Command = "GET_LOGS", Data = filterData }));
                
                if (!string.IsNullOrEmpty(response))
                {
                    using var doc = JsonDocument.Parse(response);
                    
                    // Check if response contains Data property with logs
                    if (doc.RootElement.TryGetProperty("Data", out var dataElement) &&
                        dataElement.ValueKind != JsonValueKind.Null)
                    {
                        // Deserialize the logs from the response
                        _allLogs = JsonSerializer.Deserialize<List<TransferLog>>(dataElement.GetRawText()) ?? new List<TransferLog>();
                    }
                    else
                    {
                        _allLogs = new List<TransferLog>();
                    }
                }
                else
                {
                    // No response, clear logs
                    _allLogs = new List<TransferLog>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving logs from service: {ex.Message}", "Service Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                // Fall back to local storage
                LoadLogsFromStorage();
            }
        }
        
        private void LoadLogsFromStorage()
        {
            // Get logs from local storage
            if (chkAllDates.Checked)
            {
                _allLogs = TransferLogManager.GetAllLogs();
            }
            else
            {
                _allLogs = TransferLogManager.GetLogs(dtpDate.Value);
                
                // If no logs for that date, get all logs
                if (_allLogs.Count == 0)
                {
                    _allLogs = TransferLogManager.GetAllLogs();
                    chkAllDates.Checked = true;
                }
            }
        }
        
        private void DisplayFilteredLogs()
        {
            dgvLogs.Rows.Clear();
            
            var filteredLogs = _allLogs;
            
            // Apply search filter if any
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                string search = txtSearch.Text.ToLower();
                filteredLogs = _allLogs.Where(log => 
                    (log.FileName?.ToLower().Contains(search) ?? false) || 
                    (log.Message?.ToLower().Contains(search) ?? false) ||
                    (log.SourcePath?.ToLower().Contains(search) ?? false) ||
                    (log.DestinationPath?.ToLower().Contains(search) ?? false) ||
                    (log.Status?.ToLower().Contains(search) ?? false) ||
                    (log.Protocol?.ToLower().Contains(search) ?? false)).ToList();
            }
            
            // Add rows to grid
            foreach (var log in filteredLogs)
            {
                string fileName = log.FileName ?? Path.GetFileName(log.SourcePath ?? string.Empty);
                string protocol = log.Protocol ?? "Unknown";
                long fileSize = log.FileSize;
                int durationMs = (int)log.Duration.TotalMilliseconds;
                
                var rowIndex = dgvLogs.Rows.Add(
                    log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    fileName,
                    log.Status,
                    durationMs,
                    fileSize,
                    protocol,
                    log.Message ?? log.ErrorMessage ?? string.Empty
                );
                
                // Color the row based on status
                if (log.Status?.ToLower() == "success")
                {
                    dgvLogs.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                }
                else if (log.Status?.ToLower() == "failed")
                {
                    dgvLogs.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightPink;
                }
            }
            
            // Update form title with count
            Text = $"Transfer Logs ({filteredLogs.Count} entries)";
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            DisplayFilteredLogs();
        }
        
        private void DtpDate_ValueChanged(object? sender, EventArgs e)
        {
            if (!chkAllDates.Checked)
            {
                LoadLogsAsync().ConfigureAwait(false);
            }
        }
        
        private void ChkAllDates_CheckedChanged(object? sender, EventArgs e)
        {
            dtpDate.Enabled = !chkAllDates.Checked;
            LoadLogsAsync().ConfigureAwait(false);
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            LoadLogsAsync().ConfigureAwait(false);
        }
        
        private async void BtnClear_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear ALL transfer logs?\nThis cannot be undone.", 
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Cursor = Cursors.WaitCursor;
                
                try
                {
                    // Check if service is available
                    bool serviceConnected = await _connectionManager.IsServiceConnectedAsync();
                    
                    if (serviceConnected)
                    {
                        // Send clear command to service
                        var response = await _connectionManager.SendMessageAsync(
                            JsonSerializer.Serialize(new { Command = "CLEAR_LOGS" }));
                        
                        if (!string.IsNullOrEmpty(response))
                        {
                            using var doc = JsonDocument.Parse(response);
                            
                            // Check if command succeeded
                            if (doc.RootElement.TryGetProperty("Success", out var successElement) &&
                                successElement.GetBoolean())
                            {
                                MessageBox.Show("Logs cleared successfully!", "Success", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                string message = "Unknown error";
                                if (doc.RootElement.TryGetProperty("Message", out var messageElement))
                                {
                                    message = messageElement.GetString() ?? message;
                                }
                                
                                MessageBox.Show($"Failed to clear logs: {message}", "Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        // Clear logs locally
                        TransferLogManager.ClearLogs();
                        MessageBox.Show("Logs cleared successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    // Refresh the view
                    dgvLogs.Rows.Clear();
                    _allLogs.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing logs: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }
        
        private void BtnExport_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_allLogs.Count == 0)
                {
                    MessageBox.Show("No logs to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                    sfd.DefaultExt = "csv";
                    sfd.FileName = $"TransferLogs_{DateTime.Now:yyyyMMdd}";
                    
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        // Create CSV content
                        using (var sw = new StreamWriter(sfd.FileName))
                        {
                            // Header
                            sw.WriteLine("Timestamp,Filename,Source,Destination,Status,Protocol,Size,Duration,Message");
                            
                            // Data
                            foreach (var log in _allLogs)
                            {
                                string fileName = log.FileName ?? Path.GetFileName(log.SourcePath ?? string.Empty);
                                string protocol = log.Protocol ?? "Unknown";
                                string message = log.Message ?? log.ErrorMessage ?? string.Empty;
                                
                                sw.WriteLine(
                                    $"\"{log.Timestamp}\",\"{fileName}\",\"{log.SourcePath}\",\"{log.DestinationPath}\",\"{log.Status}\"," +
                                    $"\"{protocol}\",\"{log.FileSize}\",\"{log.Duration.TotalMilliseconds}\",\"{message.Replace("\"", "\"\"")}\"");
                            }
                        }
                        
                        MessageBox.Show($"Logs exported successfully to {sfd.FileName}", "Export Complete", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting logs: {ex.Message}", "Export Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
