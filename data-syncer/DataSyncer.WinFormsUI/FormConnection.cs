using System;
using System.Drawing;
using System.Windows.Forms;
using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.WinFormsUI.Utilities;
using System.Threading.Tasks;
using System.IO;

namespace DataSyncer.WinFormsUI
{
    public class FormConnection : Form
    {
        private TextBox txtHost = null!, txtPort = null!, txtUsername = null!, txtPassword = null!, txtSourcePath = null!, txtDestinationPath = null!;
        private ComboBox cmbProtocol = null!;
        private Button btnTest = null!, btnSave = null!, btnCancel = null!;

        public FormConnection()
        {
            Text = "Connection Settings";
            Size = new Size(450, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            InitializeControls();
            LoadSettings();
        }

        private void InitializeControls()
        {
            // Host
            Label lblHost = new Label() { Text = "Host:", Location = new Point(20, 20), Size = new Size(100, 23) };
            txtHost = new TextBox() { Location = new Point(130, 20), Width = 250 };

            // Port
            Label lblPort = new Label() { Text = "Port:", Location = new Point(20, 50), Size = new Size(100, 23) };
            txtPort = new TextBox() { Location = new Point(130, 50), Width = 100 };

            // Protocol
            Label lblProtocol = new Label() { Text = "Protocol:", Location = new Point(20, 80), Size = new Size(100, 23) };
            cmbProtocol = new ComboBox() { Location = new Point(130, 80), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProtocol.Items.AddRange(new[] { "FTP", "SFTP", "LOCAL" });
            cmbProtocol.SelectedIndexChanged += CmbProtocol_SelectedIndexChanged;

            // Username
            Label lblUsername = new Label() { Text = "Username:", Location = new Point(20, 110), Size = new Size(100, 23) };
            txtUsername = new TextBox() { Location = new Point(130, 110), Width = 250 };

            // Password
            Label lblPassword = new Label() { Text = "Password:", Location = new Point(20, 140), Size = new Size(100, 23) };
            txtPassword = new TextBox() { Location = new Point(130, 140), Width = 250, PasswordChar = '*' };

            // Source Path
            Label lblSourcePath = new Label() { Text = "Source Path:", Location = new Point(20, 170), Size = new Size(100, 23) };
            txtSourcePath = new TextBox() { Location = new Point(130, 170), Width = 250 };

            // Destination Path
            Label lblDestinationPath = new Label() { Text = "Dest. Path:", Location = new Point(20, 200), Size = new Size(100, 23) };
            txtDestinationPath = new TextBox() { Location = new Point(130, 200), Width = 250 };

            // Buttons
            btnTest = new Button() { Text = "Test Connection", Location = new Point(20, 250), Size = new Size(130, 35) };
            btnSave = new Button() { Text = "Save", Location = new Point(160, 250), Size = new Size(80, 35) };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(250, 250), Size = new Size(80, 35) };

            btnTest.Click += BtnTest_Click;
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { 
                lblHost, txtHost, 
                lblPort, txtPort, 
                lblProtocol, cmbProtocol,
                lblUsername, txtUsername, 
                lblPassword, txtPassword, 
                lblSourcePath, txtSourcePath,
                lblDestinationPath, txtDestinationPath,
                btnTest, btnSave, btnCancel 
            });
        }

        private void CmbProtocol_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Set default port based on protocol
            if (cmbProtocol.SelectedItem?.ToString() == "FTP")
                txtPort.Text = "21";
            else if (cmbProtocol.SelectedItem?.ToString() == "SFTP")
                txtPort.Text = "22";
            else if (cmbProtocol.SelectedItem?.ToString() == "LOCAL")
                txtPort.Text = "0"; // No port needed for LOCAL, but set to 0
            
            // Update UI visibility based on protocol
            bool isLocal = cmbProtocol.SelectedItem?.ToString() == "LOCAL";
            txtUsername.Enabled = !isLocal;
            txtPassword.Enabled = !isLocal;
            txtPort.Enabled = !isLocal;
            // For LOCAL protocol, we only need source and destination paths
        }

        private void LoadSettings()
        {
            try
            {
                // First try to get settings from ConnectionManager (in-memory)
                var settings = ConnectionManager.Instance.CurrentSettings;
                
                // If not available, load from file
                if (settings == null)
                {
                    settings = ConfigurationManager.LoadConnectionSettings();
                }
                
                if (settings != null)
                {
                    txtHost.Text = settings.Host;
                    txtPort.Text = settings.Port.ToString();
                    txtUsername.Text = settings.Username;
                    txtPassword.Text = settings.Password;
                    txtSourcePath.Text = settings.SourcePath;
                    txtDestinationPath.Text = settings.DestinationPath;
                    
                    // Set the protocol in the dropdown
                    string protocolStr = settings.Protocol.ToString();
                    for (int i = 0; i < cmbProtocol.Items.Count; i++)
                    {
                        if (cmbProtocol.Items[i]?.ToString() == protocolStr)
                        {
                            cmbProtocol.SelectedIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    // Set defaults
                    cmbProtocol.SelectedItem = "FTP";
                    txtPort.Text = "21";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void BtnTest_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            btnTest.Enabled = false;
            btnTest.Text = "Testing...";

            try
            {
                var settings = GetConnectionSettings();
                
                // First check if the service is running
                bool serviceConnected = await ConnectionManager.Instance.TestServiceConnectionAsync();
                
                if (!serviceConnected)
                {
                    MessageBox.Show("Cannot communicate with the DataSyncer service. Make sure the service is running.", 
                        "Service Not Running", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Send test command to service through ConnectionManager
                bool success = await ConnectionManager.Instance.TestConnectionSettingsAsync(settings);
                
                if (success)
                {
                    MessageBox.Show("Connection test successful!", "Test Passed", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Connection test failed. Please check your settings and try again.", 
                        "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test Connection";
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                var settings = GetConnectionSettings();
                
                // Save to file
                ConfigurationManager.SaveConnectionSettings(settings);
                
                // Store settings in memory for other forms to access
                ConnectionManager.Instance.UpdateSettings(settings);

                // Test if we can communicate with the service
                bool serviceConnected = await ConnectionManager.Instance.TestServiceConnectionAsync();
                
                if (serviceConnected)
                {
                    // Notify service of updated settings
                    await ConnectionManager.Instance.SendConnectionUpdateAsync(settings);
                    MessageBox.Show("Settings saved and sent to service successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Settings saved, but couldn't notify the service. The service may not be running.", 
                        "Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInput()
        {
            if (cmbProtocol.SelectedItem == null)
            {
                MessageBox.Show("Please select a protocol.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbProtocol.Focus();
                return false;
            }

            bool isLocal = cmbProtocol.SelectedItem?.ToString() == "LOCAL";

            // For non-LOCAL protocols, validate host, port, and username
            if (!isLocal)
            {
                if (string.IsNullOrWhiteSpace(txtHost.Text))
                {
                    MessageBox.Show("Please enter a host.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtHost.Focus();
                    return false;
                }

                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Please enter a valid port number (1-65535).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPort.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return false;
                }
            }
            else
            {
                // For LOCAL protocol, validate source and destination paths
                if (string.IsNullOrWhiteSpace(txtSourcePath.Text))
                {
                    MessageBox.Show("Please enter a source path.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtSourcePath.Focus();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(txtDestinationPath.Text))
                {
                    MessageBox.Show("Please enter a destination path.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDestinationPath.Focus();
                    return false;
                }

                // Check if paths exist
                if (!Directory.Exists(txtSourcePath.Text))
                {
                    MessageBox.Show("Source path does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtSourcePath.Focus();
                    return false;
                }

                // Destination parent folder should exist
                string? destDir = Path.GetDirectoryName(txtDestinationPath.Text);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    MessageBox.Show("Destination directory does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtDestinationPath.Focus();
                    return false;
                }
            }

            return true;
        }

        private ConnectionSettings GetConnectionSettings()
        {
            return new ConnectionSettings
            {
                Host = txtHost.Text.Trim(),
                Port = int.Parse(txtPort.Text),
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text,
                SourcePath = txtSourcePath.Text.Trim(),
                DestinationPath = txtDestinationPath.Text.Trim(),
                Protocol = cmbProtocol.SelectedItem != null 
                    ? Enum.Parse<ProtocolType>(cmbProtocol.SelectedItem.ToString() ?? "FTP") 
                    : ProtocolType.FTP
            };
        }
    }
}
