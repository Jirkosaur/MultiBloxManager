using System;
using System.Threading;
using System.Windows.Forms;

namespace MultiBloxManager
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = "MultiBloxManager_SingleInstance_Mutex";

        [STAThread]
        private static void Main()
        {
            using var mutex = new Mutex(true, SingleInstanceMutexName, out bool isOnlyInstance);

            if (!isOnlyInstance)
            {
                MessageBox.Show(
                    "MultiBloxManager is already running in the background or system tray.",
                    "MultiBloxManager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
