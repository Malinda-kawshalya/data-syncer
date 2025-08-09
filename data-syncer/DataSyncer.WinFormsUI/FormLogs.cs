using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public class FormLogs : Form
    {
        private DataGridView dgvLogs;
        private TextBox txtSearch;
        private Button btnClear;

        public FormLogs()
        {
            Text = "Logs";
            Size = new Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblSearch = new Label() { Text = "Search:", Location = new Point(10, 15) };
            txtSearch = new TextBox() { Location = new Point(70, 12), Width = 200 };
            txtSearch.TextChanged += (s, e) => MessageBox.Show($"Searching: {txtSearch.Text}");

            dgvLogs = new DataGridView()
            {
                Location = new Point(10, 50),
                Size = new Size(560, 250),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvLogs.Columns.Add("Date", "Date");
            dgvLogs.Columns.Add("File", "File");
            dgvLogs.Columns.Add("Status", "Status");

            btnClear = new Button() { Text = "Clear Logs", Location = new Point(10, 320) };
            btnClear.Click += (s, e) => dgvLogs.Rows.Clear();

            Controls.AddRange(new Control[] { lblSearch, txtSearch, dgvLogs, btnClear });
        }
    }
}
