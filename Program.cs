using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Weighbridge
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string configPath = ConfigManager.GetConfigPath();

            if (!File.Exists(configPath))
            {
                using (ConfigurationForm configForm = new ConfigurationForm(isFirstRun: true))
                {
                    if (configForm.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show("Configuration is required to run the application.",
                            "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            ConfigManager.Load();

            while (true)
            {
                using (LoginForm loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() != DialogResult.OK) return;
                }

                using (FormSync mainForm = new FormSync())
                {
                    Application.Run(mainForm);
                    if (!mainForm.LogoutRequested) return;
                }

                SessionUser.username = null;
                SessionUser.userRole = null;
            }
        }
    }
}
