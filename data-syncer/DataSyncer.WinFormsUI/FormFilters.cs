using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using DataSyncer.Core.Models;
using DataSyncer.Core.Services;

namespace DataSyncer.WinFormsUI
{
    public class FormFilters : Form
    {
        private CheckedListBox chkFileTypes = null!;
        private NumericUpDown numMinSize = null!, numMaxSize = null!, numMinAge = null!;
        private CheckBox chkIncludeSubfolders = null!, chkDeleteAfterTransfer = null!, chkMoveToArchive = null!;
        private TextBox txtArchivePath = null!;
        private Button btnSave = null!, btnCancel = null!, btnBrowseArchive = null!;

        public FormFilters()
        {
            Text = "Filter Settings";
            Size = new Size(450, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            InitializeControls();
            LoadSettings();
        }

        private void InitializeControls()
        {
            // File Types
            Label lblTypes = new Label() { Text = "File Types:", Location = new Point(20, 20), Size = new Size(100, 20) };
            chkFileTypes = new CheckedListBox() { Location = new Point(130, 20), Size = new Size(280, 80) };
            chkFileTypes.Items.AddRange(new[] { ".txt", ".csv", ".jpg", ".png", ".pdf", ".xml", ".json", ".log" });
            chkFileTypes.CheckOnClick = true;

            // Size Filters
            Label lblMin = new Label() { Text = "Min Size (KB):", Location = new Point(20, 120), Size = new Size(100, 20) };
            numMinSize = new NumericUpDown() { 
                Location = new Point(130, 120), 
                Size = new Size(100, 20),
                Minimum = 0, 
                Maximum = 1000000,
                ThousandsSeparator = true,
                Increment = 10
            };

            Label lblMax = new Label() { Text = "Max Size (KB):", Location = new Point(20, 150), Size = new Size(100, 20) };
            numMaxSize = new NumericUpDown() { 
                Location = new Point(130, 150), 
                Size = new Size(100, 20),
                Minimum = 0, 
                Maximum = 1000000,
                ThousandsSeparator = true,
                Increment = 100,
                Value = 1000000
            };
            
            Label lblMaxNote = new Label() { 
                Text = "Note: 0 = No maximum", 
                Location = new Point(240, 150), 
                Size = new Size(170, 20),
                ForeColor = Color.Gray,
                Font = new Font(Font, FontStyle.Italic)
            };

            // Age Filter
            Label lblMinAge = new Label() { Text = "Min Age (mins):", Location = new Point(20, 180), Size = new Size(100, 20) };
            numMinAge = new NumericUpDown() { 
                Location = new Point(130, 180), 
                Size = new Size(100, 20),
                Minimum = 0, 
                Maximum = 10080, // 1 week in minutes
                Increment = 5
            };
            
            Label lblMinAgeNote = new Label() { 
                Text = "Wait before transfer", 
                Location = new Point(240, 180), 
                Size = new Size(170, 20),
                ForeColor = Color.Gray,
                Font = new Font(Font, FontStyle.Italic)
            };

            // Include Subfolders
            chkIncludeSubfolders = new CheckBox() { 
                Text = "Include Subfolders", 
                Location = new Point(20, 210), 
                Size = new Size(200, 24),
                Checked = false
            };

            // Post-transfer options
            Label lblPostTransfer = new Label() { 
                Text = "After Transfer:", 
                Location = new Point(20, 240), 
                Size = new Size(100, 20),
                Font = new Font(Font, FontStyle.Bold)
            };

            chkDeleteAfterTransfer = new CheckBox() { 
                Text = "Delete Source File", 
                Location = new Point(40, 270), 
                Size = new Size(200, 24),
                Checked = false
            };

            chkMoveToArchive = new CheckBox() { 
                Text = "Move to Archive", 
                Location = new Point(40, 300), 
                Size = new Size(200, 24),
                Checked = false
            };
            chkMoveToArchive.CheckedChanged += (s, e) => txtArchivePath.Enabled = btnBrowseArchive.Enabled = chkMoveToArchive.Checked;

            Label lblArchivePath = new Label() { Text = "Archive Path:", Location = new Point(40, 330), Size = new Size(80, 20) };
            txtArchivePath = new TextBox() { Location = new Point(130, 330), Size = new Size(240, 23), Enabled = false };
            btnBrowseArchive = new Button() { Text = "...", Location = new Point(380, 329), Size = new Size(30, 23), Enabled = false };
            btnBrowseArchive.Click += BtnBrowseArchive_Click;

            // Buttons
            btnSave = new Button() { Text = "Save", Location = new Point(240, 370), Width = 80, Height = 30 };
            btnCancel = new Button() { Text = "Cancel", Location = new Point(330, 370), Width = 80, Height = 30 };
            
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { 
                lblTypes, chkFileTypes, 
                lblMin, numMinSize, 
                lblMax, numMaxSize, lblMaxNote,
                lblMinAge, numMinAge, lblMinAgeNote,
                chkIncludeSubfolders,
                lblPostTransfer, 
                chkDeleteAfterTransfer, 
                chkMoveToArchive,
                lblArchivePath, txtArchivePath, btnBrowseArchive,
                btnSave, btnCancel 
            });
        }

        private void LoadSettings()
        {
            try
            {
                var settings = ConfigurationManager.LoadFilterSettings();
                if (settings != null)
                {
                    // File types
                    foreach (string fileType in settings.IncludeFileTypes)
                    {
                        int index = chkFileTypes.Items.IndexOf(fileType);
                        if (index >= 0)
                        {
                            chkFileTypes.SetItemChecked(index, true);
                        }
                    }

                    // Size filters
                    numMinSize.Value = settings.MinFileSizeBytes / 1024; // Convert bytes to KB
                    
                    if (settings.MaxFileSizeBytes > 0)
                    {
                        numMaxSize.Value = Math.Min(settings.MaxFileSizeBytes / 1024, numMaxSize.Maximum);
                    }
                    else
                    {
                        numMaxSize.Value = 0;
                    }
                    
                    // Age filter
                    numMinAge.Value = settings.MinAgeMinutes;
                    
                    // Other options
                    chkIncludeSubfolders.Checked = settings.IncludeSubfolders;
                    chkDeleteAfterTransfer.Checked = settings.DeleteAfterTransfer;
                    chkMoveToArchive.Checked = settings.MoveToArchiveAfterTransfer;
                    txtArchivePath.Text = settings.ArchivePath;
                    txtArchivePath.Enabled = btnBrowseArchive.Enabled = chkMoveToArchive.Checked;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading filter settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowseArchive_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Archive Folder";
                if (!string.IsNullOrEmpty(txtArchivePath.Text) && System.IO.Directory.Exists(txtArchivePath.Text))
                {
                    dialog.SelectedPath = txtArchivePath.Text;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtArchivePath.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                var settings = new FilterSettings();
                
                // Get checked file types
                foreach (int index in chkFileTypes.CheckedIndices)
                {
                    settings.IncludeFileTypes.Add(chkFileTypes.Items[index].ToString() ?? "");
                }
                
                // Size filters
                settings.MinFileSizeBytes = (long)numMinSize.Value * 1024; // Convert KB to bytes
                settings.MaxFileSizeBytes = numMaxSize.Value > 0 ? (long)numMaxSize.Value * 1024 : 0; // 0 means no max
                
                // Age filter
                settings.MinAgeMinutes = (int)numMinAge.Value;
                
                // Other options
                settings.IncludeSubfolders = chkIncludeSubfolders.Checked;
                settings.DeleteAfterTransfer = chkDeleteAfterTransfer.Checked;
                settings.MoveToArchiveAfterTransfer = chkMoveToArchive.Checked;
                settings.ArchivePath = txtArchivePath.Text;
                
                // Validate archive path if needed
                if (settings.MoveToArchiveAfterTransfer && string.IsNullOrEmpty(settings.ArchivePath))
                {
                    MessageBox.Show("Please specify an archive path when 'Move to Archive' is enabled.",
                        "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtArchivePath.Focus();
                    return;
                }
                
                // Save settings
                ConfigurationManager.SaveFilterSettings(settings);
                
                MessageBox.Show("Filter settings saved successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving filter settings: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
