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
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Check if config.xml exists
            string configPath = ConfigManager.GetConfigPath();

            if (!File.Exists(configPath))
            {
                // Config not found, show Configuration Form first
                using (ConfigurationForm configForm = new ConfigurationForm(isFirstRun: true))
                {
                    if (configForm.ShowDialog() != DialogResult.OK)
                    {
                        // User closed without saving, exit app
                        MessageBox.Show("Configuration is required to run the application.",
                            "Setup Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            ConfigManager.Load();

            using (LoginForm loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new FormSync()); // no need to pass role anymore
                }
            }
            
        }
    }
}
