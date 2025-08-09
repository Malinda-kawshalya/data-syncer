using System;
using System.Windows.Forms;

namespace DataSyncer.WinFormsUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles(); // Makes controls modern
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new FormMain()); // Start with Main Dashboard
        }
    }
}
