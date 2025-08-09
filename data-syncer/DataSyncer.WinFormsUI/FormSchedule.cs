using System;
using System.Drawing;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public class FormSchedule : Form
    {
        private DateTimePicker timePicker;
        private NumericUpDown numInterval;
        private Button btnEnable;

        public FormSchedule()
        {
            Text = "Schedule Settings";
            Size = new Size(350, 200);
            StartPosition = FormStartPosition.CenterParent;

            InitializeControls();
        }

        private void InitializeControls()
        {
            Label lblTime = new Label() { Text = "Start Time:", Location = new Point(20, 20) };
            timePicker = new DateTimePicker() { Location = new Point(120, 20), Format = DateTimePickerFormat.Time };

            Label lblInterval = new Label() { Text = "Interval (mins):", Location = new Point(20, 60) };
            numInterval = new NumericUpDown() { Location = new Point(120, 60), Minimum = 1, Maximum = 1440, Value = 60 };

            btnEnable = new Button() { Text = "Enable Schedule", Location = new Point(20, 100), Width = 120 };
            btnEnable.Click += (s, e) => MessageBox.Show("Schedule Enabled");

            Controls.AddRange(new Control[] { lblTime, timePicker, lblInterval, numInterval, btnEnable });
        }
    }
}
