using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public class FormFilters : Form
    {
        private CheckedListBox chkFileTypes;
        private NumericUpDown numMinSize, numMaxSize;
        private Button btnSave;

        public FormFilters()
        {
            Text = "Filter Settings";
            Size = new Size(350, 300);
            StartPosition = FormStartPosition.CenterParent;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblTypes = new Label() { Text = "File Types:", Location = new Point(20, 20) };
            chkFileTypes = new CheckedListBox() { Location = new Point(120, 20), Size = new Size(150, 80) };
            chkFileTypes.Items.AddRange(new[] { ".txt", ".jpg", ".png", ".pdf" });

            Label lblMin = new Label() { Text = "Min Size (MB):", Location = new Point(20, 120) };
            numMinSize = new NumericUpDown() { Location = new Point(120, 120), Minimum = 0, Maximum = 1000 };

            Label lblMax = new Label() { Text = "Max Size (MB):", Location = new Point(20, 160) };
            numMaxSize = new NumericUpDown() { Location = new Point(120, 160), Minimum = 1, Maximum = 5000 };

            btnSave = new Button() { Text = "Save Filters", Location = new Point(20, 210), Width = 100 };
            btnSave.Click += (s, e) => MessageBox.Show("Filters Saved");

            Controls.AddRange(new Control[] { lblTypes, chkFileTypes, lblMin, numMinSize, lblMax, numMaxSize, btnSave });
        }
    }
}
