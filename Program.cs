using System;
using System.Drawing;
using System.Windows.Forms;

namespace TaskbarSystemMonitor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ensure only one instance runs at a time
            using (var mutex = new System.Threading.Mutex(false, "TaskbarSystemMonitor"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("Taskbar System Monitor is already running.", "Already Running",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.Run(new TaskbarOverlayForm());
            }
        }
    }
}