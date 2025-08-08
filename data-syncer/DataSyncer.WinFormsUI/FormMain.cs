using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    public partial class FormMain : Form
    {
        public FormMain()
        { 
            InitializeComponent();

            connectionToolStripMenuItem.Click += (s, e) =>
            {
                using var frm = new FormConnection();
                frm.ShowDialog(this);
            };

            scheduleToolStripMenuItem.Click += (s, e) =>
            {
                using var frm = new FormSchedule();
                frm.ShowDialog(this);
            };

            filtersToolStripMenuItem.Click += (s, e) =>
            {
                using var frm = new FormFilters();
                frm.ShowDialog(this);
            };

            logsToolStripMenuItem.Click += (s, e) =>
            {
                using var frm = new FormLogs();
                frm.ShowDialog(this);
            };
        }
    }
}
