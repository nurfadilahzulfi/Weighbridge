using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Weighbridge.Core.Config;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class LoginForm : Form
    {
        private static readonly Color BrandGreenDark = Color.FromArgb(10, 77, 46);
        private static readonly Color BrandGreen = Color.FromArgb(22, 128, 61);
        private static readonly Color BrandGreenHover = Color.FromArgb(20, 101, 52);
        private static readonly Color BrandOrange = Color.FromArgb(247, 148, 29);
        private static readonly Color InputFill = Color.FromArgb(249, 250, 251);
        private static readonly Color InputBorder = Color.FromArgb(209, 213, 219);
        private static readonly Color MutedText = Color.FromArgb(107, 114, 128);
        private static readonly Color DangerText = Color.FromArgb(220, 38, 38);
        private static readonly Color DisabledFill = Color.FromArgb(148, 163, 184);

        private const int CardRadius = 16;
        private const int InputRadius = 8;

        private readonly IAuthService _authService;
        private bool _userFocused = false;
        private bool _passFocused = false;
        private Color _badgeFill = Color.FromArgb(220, 252, 231);
        private Color _badgeBorder = Color.FromArgb(134, 239, 172);

        public LoginForm() : this(new AuthService())
        {
        }

        public LoginForm(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? new AuthService();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            LoadImageLogo();
            LoadDbInfo();
            SetupBehaviors();
            ApplyRoundedShapes();
            CenterLoginCard();
            this.AcceptButton = btnLogin;
        }

        private void LoginForm_Resize(object sender, EventArgs e)
        {
            CenterLoginCard();
            Invalidate();
        }

        private void CenterLoginCard()
        {
            if (pnlContainer != null)
            {
                pnlContainer.Left = Math.Max(0, (this.ClientSize.Width - pnlContainer.Width) / 2);
                pnlContainer.Top = Math.Max(0, (this.ClientSize.Height - pnlContainer.Height) / 2);
            }
        }

        private void ApplyRoundedShapes()
        {
            pnlContainer.Region = new Region(RoundedRect(new Rectangle(0, 0, pnlContainer.Width, pnlContainer.Height), CardRadius));
            btnLogin.Region = new Region(RoundedRect(new Rectangle(0, 0, btnLogin.Width, btnLogin.Height), InputRadius));
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        private void LoginForm_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = ClientRectangle;
            if (area.Width <= 0 || area.Height <= 0) return;
            Rectangle card = pnlContainer.Bounds;

            using (var bg = new LinearGradientBrush(area, Color.FromArgb(246, 248, 244), Color.FromArgb(229, 239, 232), 90f))
            {
                g.FillRectangle(bg, area);
            }

            int bandHeight = Math.Max(card.Top + card.Height / 2, (int)(area.Height * 0.46));
            DrawDotTexture(g, area, bandHeight);

            using (var band = BuildBandPath(area.Width, bandHeight))
            using (var bandBrush = new LinearGradientBrush(new Rectangle(0, 0, area.Width, bandHeight + 1), Color.FromArgb(9, 58, 36), BrandGreen, 20f))
            {
                g.FillPath(bandBrush, band);

                var oldClip = g.Clip;
                g.SetClip(band);
                using (var ring = new Pen(Color.FromArgb(22, 255, 255, 255), 2f))
                using (var glow = new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
                {
                    g.FillEllipse(glow, -area.Height / 5, -area.Height / 4, area.Height / 2, area.Height / 2);
                    g.DrawEllipse(ring, area.Width - area.Height / 3, -area.Height / 6, area.Height / 2, area.Height / 2);
                    g.DrawEllipse(ring, area.Width - area.Height / 4, -area.Height / 12, area.Height / 3, area.Height / 3);
                }
                g.Clip = oldClip;
            }

            using (var accent = new Pen(Color.FromArgb(220, BrandOrange), 3f))
            using (var curve = BuildBandEdge(area.Width, bandHeight + 7))
            {
                g.DrawPath(accent, curve);
            }

            for (int i = 1; i <= 18; i++)
            {
                var shadow = new Rectangle(card.X - i, card.Y - i + 12, card.Width + i * 2, card.Height + i * 2);
                using (var path = RoundedRect(shadow, CardRadius + i))
                using (var brush = new SolidBrush(Color.FromArgb(5, 6, 40, 22)))
                {
                    g.FillPath(brush, path);
                }
            }

            DrawBrandMark(g, card);
        }

        private static GraphicsPath BuildBandPath(int width, int height)
        {
            var path = new GraphicsPath();
            path.AddLine(0, 0, width, 0);
            path.AddLine(width, 0, width, (int)(height * 0.82));
            path.AddBezier(width, (int)(height * 0.82), (int)(width * 0.68), (int)(height * 1.12), (int)(width * 0.32), (int)(height * 0.78), 0, height);
            path.CloseFigure();
            return path;
        }

        private static GraphicsPath BuildBandEdge(int width, int height)
        {
            var path = new GraphicsPath();
            path.AddBezier(width, (int)(height * 0.82), (int)(width * 0.68), (int)(height * 1.12), (int)(width * 0.32), (int)(height * 0.78), 0, height);
            return path;
        }

        private static void DrawDotTexture(Graphics g, Rectangle area, int fromY)
        {
            const int step = 22;
            using (var dot = new SolidBrush(Color.FromArgb(26, BrandGreen)))
            {
                for (int y = fromY - fromY % step; y < area.Height; y += step)
                    for (int x = step / 2; x < area.Width; x += step)
                        g.FillEllipse(dot, x, y, 2.4f, 2.4f);
            }
        }

        private void DrawBrandMark(Graphics g, Rectangle card)
        {
            if (card.Top < 96) return;

            var tile = new Rectangle(32, 26, 40, 40);
            using (var path = RoundedRect(tile, 10))
            using (var white = new SolidBrush(Color.White))
            {
                g.FillPath(white, path);
            }
            if (pbLogo.Image != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(pbLogo.Image, new Rectangle(tile.X + 5, tile.Y + 5, tile.Width - 10, tile.Height - 10));
            }

            using (var titleFont = new Font("Segoe UI", 13F, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 9F))
            using (var white = new SolidBrush(Color.White))
            using (var soft = new SolidBrush(Color.FromArgb(200, 209, 250, 229)))
            {
                g.DrawString("WEIGHBRIDGE", titleFont, white, tile.Right + 12, tile.Top - 1);
                g.DrawString("PT Tanimas Resources International", subFont, soft, tile.Right + 13, tile.Top + 22);
            }
        }

        private void PnlLeft_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = pnlLeft.ClientRectangle;

            using (var bg = new LinearGradientBrush(area, BrandGreen, BrandGreenDark, 60f))
            {
                g.FillRectangle(bg, area);
            }

            using (var glow = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
            {
                g.FillEllipse(glow, -90, -110, 280, 280);
                g.FillEllipse(glow, area.Width - 150, area.Height - 170, 260, 260);
            }

            var tile = new Rectangle(pbLogo.Left - 14, pbLogo.Top - 14, pbLogo.Width + 28, pbLogo.Height + 28);
            using (var path = RoundedRect(tile, 20))
            using (var white = new SolidBrush(Color.White))
            {
                g.FillPath(white, path);
            }

            using (var accent = new SolidBrush(BrandOrange))
            {
                g.FillRectangle(accent, 0, area.Height - 5, area.Width, 5);
            }
        }

        private void PnlDbBadge_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnlDbBadge.Width - 1, pnlDbBadge.Height - 1);
            using (var path = RoundedRect(rect, 6))
            using (var fill = new SolidBrush(_badgeFill))
            using (var pen = new Pen(_badgeBorder))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }

        private void SetupBehaviors()
        {
            btnLogin.MouseEnter += (s, e) =>
            {
                if (btnLogin.Enabled)
                    btnLogin.BackColor = BrandGreenHover;
            };

            btnLogin.MouseLeave += (s, e) =>
            {
                if (btnLogin.Enabled)
                    btnLogin.BackColor = BrandGreen;
            };

            btnExit.MouseEnter += (s, e) => btnExit.ForeColor = DangerText;
            btnExit.MouseLeave += (s, e) => btnExit.ForeColor = MutedText;
        }

        public void LoadImageLogo()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(baseDirectory, "Assets", "logo-tanimas.png");

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
            }
        }

        private void LoadDbInfo()
        {
            try
            {
                if (ConfigManager.sboConfig != null && !string.IsNullOrWhiteSpace(ConfigManager.sboConfig.database))
                {
                    string dbName = ConfigManager.sboConfig.database.ToUpper();
                    bool isSandbox = dbName.Contains("TEST") || dbName.Contains("TRAINING") || dbName.Contains("DEV");
                    lblDbBadge.Text = $"Database: {ConfigManager.sboConfig.database}  •  {(isSandbox ? "SANDBOX" : "PRODUCTION")}";
                    SetBadgeStyle(isSandbox);
                }
                else
                {
                    lblDbBadge.Text = "Database: Belum Terkonfigurasi";
                    SetBadgeStyle(false);
                }
            }
            catch
            {
                lblDbBadge.Text = "Database: Default";
            }
        }

        private void SetBadgeStyle(bool isSandbox)
        {
            if (isSandbox)
            {
                _badgeFill = Color.FromArgb(220, 252, 231);
                _badgeBorder = Color.FromArgb(134, 239, 172);
                lblDbBadge.ForeColor = Color.FromArgb(21, 128, 61);
            }
            else
            {
                _badgeFill = Color.FromArgb(255, 237, 213);
                _badgeBorder = Color.FromArgb(253, 186, 116);
                lblDbBadge.ForeColor = Color.FromArgb(194, 65, 12);
            }
            pnlDbBadge.Invalidate();
        }

        private void PanelBorder_Paint(object sender, PaintEventArgs e)
        {
            Panel pnl = sender as Panel;
            if (pnl == null) return;

            bool isFocused = (pnl == pnlUserBorder && _userFocused) || (pnl == pnlPassBorder && _passFocused);
            Color fillColor = isFocused ? Color.White : InputFill;
            Color borderColor = isFocused ? BrandGreen : InputBorder;
            float borderWidth = isFocused ? 2f : 1f;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(1, 1, pnl.Width - 3, pnl.Height - 3);
            using (var path = RoundedRect(rect, InputRadius))
            using (var fill = new SolidBrush(fillColor))
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }
        }

        private void Input_Enter(object sender, EventArgs e)
        {
            HideError();
            if (sender == tbLoginUsrnm)
            {
                _userFocused = true;
                tbLoginUsrnm.BackColor = Color.White;
                pnlUserBorder.Invalidate();
            }
            else if (sender == tbLoginPass)
            {
                _passFocused = true;
                tbLoginPass.BackColor = Color.White;
                pnlPassBorder.Invalidate();
            }
        }

        private void Input_Leave(object sender, EventArgs e)
        {
            if (sender == tbLoginUsrnm)
            {
                _userFocused = false;
                tbLoginUsrnm.BackColor = InputFill;
                pnlUserBorder.Invalidate();
            }
            else if (sender == tbLoginPass)
            {
                _passFocused = false;
                tbLoginPass.BackColor = InputFill;
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
            lblErrorMessage.Text = message;
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
                btnLogin.BackColor = DisabledFill;
                this.Cursor = Cursors.WaitCursor;
            }
            else
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "MASUK KE SISTEM";
                btnLogin.BackColor = BrandGreen;
                this.Cursor = Cursors.Default;
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            HideError();
            string username = tbLoginUsrnm.Text.Trim();
            string password = tbLoginPass.Text.Trim();

            SetLoading(true);
            try
            {
                var result = _authService.Authenticate(username, password);
                if (!result.Success)
                {
                    ShowError(result.ErrorMessage);
                    if (string.IsNullOrEmpty(username))
                    {
                        tbLoginUsrnm.Focus();
                    }
                    else if (string.IsNullOrEmpty(password))
                    {
                        tbLoginPass.Focus();
                    }
                    return;
                }

                SessionUser.username = result.FullName;
                SessionUser.userRole = result.Role;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("Terjadi kesalahan saat verifikasi: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }
    }
}
