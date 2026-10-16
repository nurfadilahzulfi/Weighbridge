using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Weighbridge.Core.Config;

namespace Weighbridge
{
    public partial class FormSync : Form
    {
        private static readonly Color BrandGreen = Color.FromArgb(22, 128, 61);
        private static readonly Color BrandGreenDark = Color.FromArgb(10, 77, 46);
        private static readonly Color BrandOrange = Color.FromArgb(247, 148, 29);
        private static readonly Color PageBack = Color.FromArgb(243, 246, 244);
        private static readonly Color SectionText = Color.FromArgb(75, 85, 99);
        private static readonly CultureInfo Indonesian = new CultureInfo("id-ID");

        private const int TileMinWidth = 250;
        private const int TileMaxWidth = 360;
        private const int TileHeight = 100;
        private const int TileGap = 16;
        private const int HeaderHeight = 104;

        private readonly Panel _header = new BufferedPanel();
        private readonly Panel _content = new BufferedPanel();
        private readonly Label _lblGreeting = new Label();
        private readonly Label _lblSubtitle = new Label();
        private readonly Label _lblClock = new Label();
        private readonly Label _lblDb = new Label();
        private readonly HeaderButton _btnLogout = new HeaderButton();
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly Timer _clockTimer = new Timer { Interval = 1000 };
        private readonly List<Tuple<Label, List<HomeTile>>> _sections = new List<Tuple<Label, List<HomeTile>>>();
        private bool _isSandbox;

        public bool LogoutRequested { get; private set; }

        public FormSync()
        {
            InitializeComponent();
        }

        private void FormSync_Load(object sender, EventArgs e)
        {
            ApplyMenuPermissions();
            SetupShortcuts();
            LoadBrandingImage();
            BuildDashboard();
            LayoutDashboard();
            Weighbridge.Core.Helpers.WatermarkHelper.Apply(_content);
        }

        private void FormSync_Resize(object sender, EventArgs e)
        {
            LayoutDashboard();
        }

        private void LoadBrandingImage()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] candidatePaths = new string[]
                {
                    Path.Combine(baseDir, "Assets", "logo-tanimas.png"),
                    Path.Combine(baseDir, "..", "..", "Assets", "logo-tanimas.png"),
                    Path.Combine(baseDir, "..", "..", "..", "Assets", "logo-tanimas.png"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo-tanimas.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo-tanimas.png"),
                    @"c:\Soltius\Development\Add On\Weighbridge (TESTING) C\Weighbridge\Assets\logo-tanimas.png",
                    @"c:\Soltius\Development\Add On\Weighbridge (TESTING) D\Weighbridge\Assets\logo-tanimas.png"
                };

                foreach (string path in candidatePaths)
                {
                    if (File.Exists(path))
                    {
                        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            pbLogo.Image = new Bitmap(stream);
                        }
                        break;
                    }
                }
            }
            catch
            {
            }
        }

        private void ApplyMenuPermissions()
        {
            konfigurasiToolStripMenuItem.Visible = Weighbridge.Core.Services.UserRoles.CanAccessConfiguration(SessionUser.userRole);
            weighbridgeToolStripMenuItem.Visible = true;
            updateGRPODOToolStripMenuItem.Visible = true;
        }

        private void SetupShortcuts()
        {
            mainMenuStrip.Visible = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ToolStripMenuItem target = null;
            switch (keyData)
            {
                case Keys.Control | Keys.L:
                    Logout();
                    return true;
                case Keys.F2: target = timbangMasukToolStripMenuItem; break;
                case Keys.F3: target = timbangKeluarToolStripMenuItem; break;
                case Keys.F4: target = reprintToolStripMenuItem; break;
                case Keys.F5: target = reportToolStripMenuItem; break;
            }

            if (target != null)
            {
                if (IsMenuAvailable(target)) target.PerformClick();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Logout()
        {
            var openForms = Application.OpenForms.Cast<Form>().Where(f => f != this && f.Visible).ToList();
            string message = "Keluar dari akun " + (SessionUser.username ?? "ini") + " dan kembali ke halaman login?";
            if (openForms.Count > 0)
            {
                var names = openForms.Select(f => "- " + f.Text.Split(new[] { " - " }, StringSplitOptions.None)[0]);
                message = "Masih ada jendela yang terbuka:\n" + string.Join("\n", names) +
                          "\n\nJendela tersebut akan ditutup dan data yang belum disimpan akan hilang.\nTetap logout?";
            }

            if (MessageBox.Show(this, message, "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            foreach (var form in openForms) form.Close();

            if (Application.OpenForms.Cast<Form>().Any(f => f != this && f.Visible))
            {
                MessageBox.Show(this, "Masih ada jendela yang belum bisa ditutup. Selesaikan atau tutup jendela tersebut terlebih dahulu.",
                    "Logout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LogoutRequested = true;
            Close();
        }

        private void BuildDashboard()
        {
            SuspendLayout();
            BackColor = PageBack;
            lblWatermark.ForeColor = Color.FromArgb(107, 114, 128);

            _header.Dock = DockStyle.Top;
            _header.Height = HeaderHeight;
            _header.Paint += Header_Paint;
            _header.Resize += (s, e) => PositionHeaderRight();

            Controls.Remove(pbLogo);
            pbLogo.BackColor = Color.White;
            pbLogo.Size = new Size(52, 52);
            pbLogo.Location = new Point(32, 26);
            _header.Controls.Add(pbLogo);

            _lblGreeting.AutoSize = true;
            _lblGreeting.BackColor = Color.Transparent;
            _lblGreeting.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            _lblGreeting.ForeColor = Color.White;
            _lblGreeting.Location = new Point(108, 24);
            _lblGreeting.Text = "Selamat datang, " + (string.IsNullOrWhiteSpace(SessionUser.username) ? "Operator" : SessionUser.username);
            _header.Controls.Add(_lblGreeting);

            _lblSubtitle.AutoSize = true;
            _lblSubtitle.BackColor = Color.Transparent;
            _lblSubtitle.Font = new Font("Segoe UI", 9.5F);
            _lblSubtitle.ForeColor = Color.FromArgb(209, 250, 229);
            _lblSubtitle.Location = new Point(110, 60);
            _header.Controls.Add(_lblSubtitle);

            _lblClock.AutoSize = true;
            _lblClock.BackColor = Color.Transparent;
            _lblClock.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            _lblClock.ForeColor = Color.White;
            _lblClock.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _header.Controls.Add(_lblClock);

            _lblDb.AutoSize = true;
            _lblDb.BackColor = Color.Transparent;
            _lblDb.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _lblDb.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _lblDb.Padding = new Padding(10, 4, 10, 4);
            _header.Controls.Add(_lblDb);
            ApplyDbInfo();

            _btnLogout.Text = "Logout";
            _btnLogout.Size = new Size(104, 34);
            _btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnLogout.Click += (s, e) => Logout();
            _toolTip.SetToolTip(_btnLogout, "Keluar dan kembali ke halaman login (Ctrl+L)");
            _header.Controls.Add(_btnLogout);

            _content.Dock = DockStyle.Fill;
            _content.AutoScroll = true;
            _content.BackColor = PageBack;

            AddSection("OPERASIONAL TIMBANG", new[]
            {
                CreateTile("Timbang Masuk", "Catat berat masuk truk, data SPB kebun, dan split kontrak.", "\uE896", "F2", BrandGreen, timbangMasukToolStripMenuItem),
                CreateTile("Timbang Keluar", "Catat berat keluar lalu kirim GRPO atau Delivery ke SAP.", "\uE898", "F3", Color.FromArgb(234, 88, 12), timbangKeluarToolStripMenuItem),
                CreateTile("Reprint Tiket", "Cetak ulang kartu timbang yang sudah tersimpan.", "\uE749", "F4", Color.FromArgb(14, 116, 144), reprintToolStripMenuItem),
                CreateTile("Report", "Lihat dan ekspor laporan data timbangan.", "\uE9D2", "F5", Color.FromArgb(79, 70, 229), reportToolStripMenuItem)
            });

            AddSection("UPDATE DOKUMEN SAP", new[]
            {
                CreateTile("Update Timbang Masuk", "Timbang masuk untuk GRPO atau Delivery yang sudah ada.", "\uE895", null, Color.FromArgb(21, 128, 61), timbangMasukUpdateTS),
                CreateTile("Update Timbang Keluar", "Perbarui berat keluar pada GRPO atau Delivery.", "\uE895", null, Color.FromArgb(194, 65, 12), timbangKeluarUpdateTS)
            });

            AddSection("PENGATURAN", new[]
            {
                CreateTile("Konfigurasi", "Koneksi database, SAP, dan timbangan.", "\uE713", null, Color.FromArgb(71, 85, 105), konfigurasiToolStripMenuItem)
            });

            Controls.Add(_header);
            Controls.Add(_content);
            Controls.SetChildIndex(_content, 0);
            Controls.SetChildIndex(_header, 1);
            Controls.SetChildIndex(lblWatermark, 2);
            Controls.SetChildIndex(mainMenuStrip, 3);

            _clockTimer.Tick += (s, e) => UpdateClock();
            UpdateClock();
            _clockTimer.Start();
            FormClosed += (s, e) =>
            {
                _clockTimer.Dispose();
                _toolTip.Dispose();
            };
            ResumeLayout(true);
        }

        private HomeTile CreateTile(string title, string description, string glyph, string shortcut, Color accent, ToolStripMenuItem menuItem)
        {
            var tile = new HomeTile
            {
                Title = title,
                Description = description,
                Glyph = glyph,
                Shortcut = shortcut,
                Accent = accent,
                Size = new Size(TileMinWidth, TileHeight),
                BackColor = PageBack,
                Tag = menuItem
            };
            tile.Click += (s, e) => menuItem.PerformClick();
            return tile;
        }

        private void AddSection(string title, IEnumerable<HomeTile> tiles)
        {
            var label = new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = SectionText,
                BackColor = Color.Transparent
            };
            var list = tiles.ToList();
            _content.Controls.Add(label);
            foreach (var tile in list) _content.Controls.Add(tile);
            _sections.Add(Tuple.Create(label, list));
        }

        private static bool IsMenuAvailable(ToolStripMenuItem item)
        {
            ToolStripItem current = item;
            while (current != null)
            {
                if (!current.Available) return false;
                current = current.OwnerItem;
            }
            return true;
        }

        private void LayoutDashboard()
        {
            if (_sections.Count == 0) return;

            int available = Math.Max(TileMinWidth, _content.ClientSize.Width - 48);
            int cols = Math.Max(1, Math.Min(4, (available + TileGap) / (TileMinWidth + TileGap)));
            if (cols == 3) cols = 2;
            int tileWidth = Math.Min(TileMaxWidth, (available - (cols - 1) * TileGap) / cols);
            int blockWidth = cols * tileWidth + (cols - 1) * TileGap;
            int left = Math.Max(24, (_content.ClientSize.Width - blockWidth) / 2);
            int offsetY = _content.AutoScrollPosition.Y;
            int y = 24;

            _content.SuspendLayout();
            foreach (var section in _sections)
            {
                var visible = section.Item2.Where(t => IsMenuAvailable((ToolStripMenuItem)t.Tag)).ToList();
                foreach (var tile in section.Item2) tile.Visible = visible.Contains(tile);
                section.Item1.Visible = visible.Count > 0;
                if (visible.Count == 0) continue;

                section.Item1.Location = new Point(left + 2, y + offsetY);
                y += 28;

                for (int i = 0; i < visible.Count; i++)
                {
                    int col = i % cols, row = i / cols;
                    visible[i].Bounds = new Rectangle(left + col * (tileWidth + TileGap), y + row * (TileHeight + TileGap) + offsetY, tileWidth, TileHeight);
                }

                int rows = (visible.Count + cols - 1) / cols;
                y += rows * (TileHeight + TileGap) + 14;
            }
            _content.AutoScrollMinSize = new Size(0, y + 8);
            _content.ResumeLayout(false);
        }

        private void ApplyDbInfo()
        {
            string db = ConfigManager.sboConfig?.database;
            if (string.IsNullOrWhiteSpace(db))
            {
                _lblDb.Text = "Database belum terkonfigurasi";
                _isSandbox = false;
            }
            else
            {
                string upper = db.ToUpperInvariant();
                _isSandbox = upper.Contains("TEST") || upper.Contains("TRAINING") || upper.Contains("DEV");
                _lblDb.Text = db + "  •  " + (_isSandbox ? "SANDBOX" : "PRODUCTION");
            }
            _lblDb.ForeColor = _isSandbox ? Color.FromArgb(21, 128, 61) : Color.FromArgb(194, 65, 12);
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            string role = Weighbridge.Core.Services.UserRoles.DisplayName(SessionUser.userRole);
            _lblClock.Text = now.ToString("HH:mm:ss");
            _lblSubtitle.Text = role + "  •  " + now.ToString("dddd, dd MMMM yyyy", Indonesian);
            PositionHeaderRight();
        }

        private void PositionHeaderRight()
        {
            int right = _header.ClientSize.Width - 28;
            _btnLogout.Location = new Point(right - _btnLogout.Width, 22);
            _lblClock.Location = new Point(_btnLogout.Left - 18 - _lblClock.PreferredWidth, 18);
            _lblDb.Location = new Point(right - _lblDb.PreferredWidth, 64);
            _header.Invalidate(new Rectangle(_lblDb.Left - 2, _lblDb.Top - 2, _lblDb.Width + 4, _lblDb.Height + 4));
        }

        private void Header_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle area = _header.ClientRectangle;
            if (area.Width <= 0 || area.Height <= 0) return;

            using (var bg = new LinearGradientBrush(area, BrandGreen, BrandGreenDark, 0f))
            {
                g.FillRectangle(bg, area);
            }
            using (var glow = new SolidBrush(Color.FromArgb(16, 255, 255, 255)))
            {
                g.FillEllipse(glow, area.Width - 260, -120, 300, 240);
            }
            using (var accent = new SolidBrush(BrandOrange))
            {
                g.FillRectangle(accent, 0, area.Height - 4, area.Width, 4);
            }

            using (var path = RoundedRect(new Rectangle(pbLogo.Left - 8, pbLogo.Top - 8, pbLogo.Width + 16, pbLogo.Height + 16), 14))
            using (var white = new SolidBrush(Color.White))
            {
                g.FillPath(white, path);
            }

            if (_lblDb.Width > 0)
            {
                Color fill = _isSandbox ? Color.FromArgb(220, 252, 231) : Color.FromArgb(255, 237, 213);
                using (var path = RoundedRect(_lblDb.Bounds, _lblDb.Height / 2))
                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void konfigurasiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigurationForm configurationForm = new ConfigurationForm();
            configurationForm.Show();
        }

        private void timbangMasukToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WeighbridgeForm weighbridgeForm = new WeighbridgeForm();
            weighbridgeForm.Show();
        }

        private void timbangKeluarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TimbangKeluarForm timbangKeluarForm = new TimbangKeluarForm();
            timbangKeluarForm.Show();
        }

        private void reprintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReprintTiket reprintTiket = new ReprintTiket();
            reprintTiket.Show();
        }

        private void reportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm();
            reportForm.Show();
        }

        private void timbangMasukUpdateTS_Click(object sender, EventArgs e)
        {
            timbangMasukUpdateGRPODOForm timbangMasukUpdateGRPODOForm = new timbangMasukUpdateGRPODOForm();
            timbangMasukUpdateGRPODOForm.Show();
        }

        private void timbangKeluarUpdateTS_Click(object sender, EventArgs e)
        {
            timbangKeluarUpdateGRPODOForm timbangKeluarUpdateGRPODOForm = new timbangKeluarUpdateGRPODOForm();
            timbangKeluarUpdateGRPODOForm.Show();
        }

        private sealed class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }
        }

        private sealed class HeaderButton : Control
        {
            private static readonly bool HasIconFont = FontFamily.Families.Any(f => f.Name == "Segoe MDL2 Assets");
            private bool _hover;

            public HeaderButton()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
                BackColor = Color.Transparent;
                ForeColor = Color.White;
                Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                Cursor = Cursors.Hand;
                TabStop = true;
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
            protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    OnClick(EventArgs.Empty);
                    e.Handled = true;
                }
                base.OnKeyDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool active = _hover || (Focused && ShowFocusCues);
                var rect = new Rectangle(1, 1, Width - 3, Height - 3);

                using (var path = RoundedRect(rect, rect.Height / 2))
                using (var fill = new SolidBrush(active ? Color.White : Color.FromArgb(38, 255, 255, 255)))
                using (var border = new Pen(Color.FromArgb(active ? 255 : 150, 255, 255, 255)))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }

                Color textColor = active ? Color.FromArgb(185, 28, 28) : ForeColor;
                int iconWidth = HasIconFont ? 18 : 0;
                Size textSize = TextRenderer.MeasureText(Text, Font);
                int startX = (Width - (iconWidth + 6 + textSize.Width)) / 2;

                if (HasIconFont)
                {
                    using (var iconFont = new Font("Segoe MDL2 Assets", 10F))
                    {
                        TextRenderer.DrawText(g, "", iconFont, new Rectangle(startX, 0, iconWidth, Height), textColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                }
                TextRenderer.DrawText(g, Text, Font, new Rectangle(startX + iconWidth + 6, 0, textSize.Width, Height), textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private sealed class HomeTile : Control
        {
            private static readonly bool HasIconFont = FontFamily.Families.Any(f => f.Name == "Segoe MDL2 Assets");
            private bool _hover;

            public string Title { get; set; }
            public string Description { get; set; }
            public string Glyph { get; set; }
            public string Shortcut { get; set; }
            public Color Accent { get; set; }

            public HomeTile()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
                Cursor = Cursors.Hand;
                TabStop = true;
            }

            protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
            protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
            protected override void OnMouseDown(MouseEventArgs e) { Focus(); base.OnMouseDown(e); }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
                {
                    OnClick(EventArgs.Empty);
                    e.Handled = true;
                }
                base.OnKeyDown(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(BackColor);

                bool active = _hover || (Focused && ShowFocusCues);
                var card = new Rectangle(1, 1, Width - 3, Height - 4);

                if (active)
                {
                    using (var shadowPath = RoundedRect(new Rectangle(card.X + 1, card.Y + 3, card.Width, card.Height), 12))
                    using (var shadow = new SolidBrush(Color.FromArgb(22, 15, 40, 25)))
                    {
                        g.FillPath(shadow, shadowPath);
                    }
                }

                using (var path = RoundedRect(card, 12))
                using (var fill = new SolidBrush(Color.White))
                using (var border = new Pen(active ? Accent : Color.FromArgb(229, 231, 235), active ? 1.6f : 1f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }

                var circle = new Rectangle(18, (Height - 48) / 2 - 1, 48, 48);
                using (var brush = new SolidBrush(Accent))
                {
                    g.FillEllipse(brush, circle);
                }

                string icon = HasIconFont ? Glyph : (string.IsNullOrEmpty(Title) ? "" : Title.Substring(0, 1));
                using (var iconFont = HasIconFont ? new Font("Segoe MDL2 Assets", 16F) : new Font("Segoe UI", 15F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, icon, iconFont, circle, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }

                int textLeft = 80;
                int textWidth = Width - textLeft - 14;

                if (!string.IsNullOrEmpty(Shortcut))
                {
                    using (var keyFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
                    {
                        Size size = TextRenderer.MeasureText(Shortcut, keyFont);
                        var pill = new Rectangle(Width - size.Width - 24, 12, size.Width + 10, size.Height + 2);
                        using (var pillPath = RoundedRect(pill, 5))
                        using (var pillBrush = new SolidBrush(Color.FromArgb(243, 244, 246)))
                        {
                            g.FillPath(pillBrush, pillPath);
                        }
                        TextRenderer.DrawText(g, Shortcut, keyFont, pill, Color.FromArgb(75, 85, 99),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        textWidth = pill.Left - textLeft - 6;
                    }
                }

                using (var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, Title, titleFont, new Rectangle(textLeft, 16, textWidth, 22), Color.FromArgb(17, 24, 39),
                        TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
                using (var descFont = new Font("Segoe UI", 8.5F))
                {
                    TextRenderer.DrawText(g, Description, descFont, new Rectangle(textLeft, 42, Width - textLeft - 14, Height - 52), Color.FromArgb(107, 114, 128),
                        TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }
        }
    }
}
