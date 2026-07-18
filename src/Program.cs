using System;
using System.Threading;
using System.Windows.Forms;

namespace Multikeys
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Verhindert, dass die App mehrfach gestartet wird.
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "Multikeys_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Multikeys laeuft bereits.", "Multikeys",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unerwarteter Fehler:\n\n" + ex, "Multikeys",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
