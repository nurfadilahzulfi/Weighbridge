using System;
using Sap.Data.Hana;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;
using Weighbridge.Core.Config;
using Weighbridge.Core.Encryptions;

namespace Weighbridge
{
    public partial class LoginForm : Form
    {
        private bool _userFocused = false;
        private bool _passFocused = false;

        public LoginForm()
        {
            InitializeComponent();
            LoadImageLogo();
            LoadDbInfo();
            SetupBehaviors();
            CenterLoginCard();
            this.AcceptButton = btnLogin;
        }

        private void LoginForm_Resize(object sender, EventArgs e)
        {
            CenterLoginCard();
        }

        private void CenterLoginCard()
        {
            if (pnlContainer != null)
            {
                pnlContainer.Left = Math.Max(0, (this.ClientSize.Width - pnlContainer.Width) / 2);
                pnlContainer.Top = Math.Max(0, (this.ClientSize.Height - pnlContainer.Height) / 2);
            }
        }

        private void SetupBehaviors()
        {
            // Hover effect for login button
            btnLogin.MouseEnter += (s, e) =>
            {
                if (btnLogin.Enabled)
                    btnLogin.BackColor = Color.FromArgb(20, 68, 128);
            };

            btnLogin.MouseLeave += (s, e) =>
            {
                if (btnLogin.Enabled)
                    btnLogin.BackColor = Color.FromArgb(26, 86, 160);
            };

            // Exit button hover
            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = Color.FromArgb(71, 85, 105);
            btnExit.MouseLeave += (s, e) => btnExit.ForeColor = Color.FromArgb(148, 163, 184);
        }

        public void LoadImageLogo()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(baseDirectory, "Assets", "logo-tanimas.png");

                // Fallback check if running in project source directory
                if (!File.Exists(imagePath))
                {
                    imagePath = Path.Combine(baseDirectory, "..", "..", "Assets", "logo-tanimas.png");
                }

                if (File.Exists(imagePath))
                {
                    pbLogo.Image = Image.FromFile(imagePath);
                }
            }
            catch
            {
                // Graceful fallback if image load fails
            }
        }

        private void LoadDbInfo()
        {
            try
            {
                if (ConfigManager.sboConfig != null && !string.IsNullOrWhiteSpace(ConfigManager.sboConfig.database))
                {
                    string dbName = ConfigManager.sboConfig.database.ToUpper();
                    string envType = dbName.Contains("TRAINING") || dbName.Contains("TESTING") ? "[SANDBOX]" : "[PRODUCTION]";
                    lblDbBadge.Text = $"🗄️ Target DB: {ConfigManager.sboConfig.database} {envType}";
                }
                else
                {
                    lblDbBadge.Text = "🗄️ Target DB: Belum Terkonfigurasi";
                }
            }
            catch
            {
                lblDbBadge.Text = "🗄️ Target DB: Default";
            }
        }

        private void PanelBorder_Paint(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null) return;

            bool isFocused = (pnl == pnlUserBorder && _userFocused) || (pnl == pnlPassBorder && _passFocused);
            Color borderColor = isFocused ? Color.FromArgb(26, 86, 160) : Color.FromArgb(203, 213, 225);
            int borderWidth = isFocused ? 2 : 1;

            using (Pen pen = new Pen(borderColor, borderWidth))
            {
                Rectangle rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void Input_Enter(object sender, EventArgs e)
        {
            HideError();
            if (sender == tbLoginUsrnm)
            {
                _userFocused = true;
                pnlUserBorder.BackColor = Color.White;
                tbLoginUsrnm.BackColor = Color.White;
                pnlUserBorder.Invalidate();
            }
            else if (sender == tbLoginPass)
            {
                _passFocused = true;
                pnlPassBorder.BackColor = Color.White;
                tbLoginPass.BackColor = Color.White;
                pnlPassBorder.Invalidate();
            }
        }

        private void Input_Leave(object sender, EventArgs e)
        {
            if (sender == tbLoginUsrnm)
            {
                _userFocused = false;
                pnlUserBorder.BackColor = Color.FromArgb(248, 250, 252);
                tbLoginUsrnm.BackColor = Color.FromArgb(248, 250, 252);
                pnlUserBorder.Invalidate();
            }
            else if (sender == tbLoginPass)
            {
                _passFocused = false;
                pnlPassBorder.BackColor = Color.FromArgb(248, 250, 252);
                tbLoginPass.BackColor = Color.FromArgb(248, 250, 252);
                pnlPassBorder.Invalidate();
            }
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            tbLoginPass.PasswordChar = chkShowPassword.Checked ? '\0' : '●';
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ShowError(string message)
        {
            lblErrorMessage.Text = "⚠️ " + message;
            lblErrorMessage.Visible = true;
        }

        private void HideError()
        {
            lblErrorMessage.Visible = false;
            lblErrorMessage.Text = "";
        }

        private void SetLoading(bool isLoading)
        {
            if (isLoading)
            {
                btnLogin.Enabled = false;
                btnLogin.Text = "MEMVERIFIKASI...";
                btnLogin.BackColor = Color.FromArgb(100, 116, 139);
                this.Cursor = Cursors.WaitCursor;
            }
            else
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "MASUK KE SISTEM";
                btnLogin.BackColor = Color.FromArgb(26, 86, 160);
                this.Cursor = Cursors.Default;
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            HideError();
            string username = tbLoginUsrnm.Text.Trim();
            string password = tbLoginPass.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Silakan masukkan Username operator.");
                tbLoginUsrnm.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("Silakan masukkan Kata Sandi.");
                tbLoginPass.Focus();
                return;
            }

            SetLoading(true);

            try
            {
                HANAConnetion hanaConn = new HANAConnetion();
                string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                using (var connection = new HanaConnection(connectionString))
                {
                    connection.Open();

                    string safeUser = username.Replace("'", "''");
                    string safePass = password.Replace("'", "''");
                    string queryString = $"CALL SOL_SP_ADDON_WB_LOGIN_GET_ROLE('{safeUser}','{safePass}')";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                ShowError("User atau Password salah! Periksa kembali kredensial Anda.");
                                SetLoading(false);
                                return;
                            }

                            while (reader.Read())
                            {
                                string role = reader["USERROLE"].ToString();
                                SessionUser.username = reader["NAME"].ToString();
                                SessionUser.userRole = role;

                                this.DialogResult = DialogResult.OK;
                                this.Close();
                                return;
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                ShowError("Gagal terhubung ke SAP HANA: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}
