using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public class FormConnection : Form
    {
        private TextBox txtHost, txtPort, txtUsername, txtPassword;
        private ComboBox cmbProtocol;
        private Button btnTest, btnSave;

        public FormConnection()
        {
            Text = "Connection Settings";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblHost = new Label() { Text = "Host:", Location = new Point(20, 20) };
            txtHost = new TextBox() { Location = new Point(120, 20), Width = 200 };

            Label lblPort = new Label() { Text = "Port:", Location = new Point(20, 60) };
            txtPort = new TextBox() { Location = new Point(120, 60), Width = 200 };

            Label lblUsername = new Label() { Text = "Username:", Location = new Point(20, 100) };
            txtUsername = new TextBox() { Location = new Point(120, 100), Width = 200 };

            Label lblPassword = new Label() { Text = "Password:", Location = new Point(20, 140) };
            txtPassword = new TextBox() { Location = new Point(120, 140), Width = 200, PasswordChar = '*' };

            Label lblProtocol = new Label() { Text = "Protocol:", Location = new Point(20, 180) };
            cmbProtocol = new ComboBox() { Location = new Point(120, 180), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProtocol.Items.AddRange(new[] { "FTP", "SFTP" });

            btnTest = new Button() { Text = "Test Connection", Location = new Point(20, 220), Width = 130 };
            btnSave = new Button() { Text = "Save", Location = new Point(160, 220), Width = 80 };

            btnTest.Click += (s, e) => MessageBox.Show("Test Connection Clicked");
            btnSave.Click += (s, e) => MessageBox.Show("Settings Saved");

            Controls.AddRange(new Control[] { lblHost, txtHost, lblPort, txtPort, lblUsername, txtUsername, lblPassword, txtPassword, lblProtocol, cmbProtocol, btnTest, btnSave });
        }
    }
}
