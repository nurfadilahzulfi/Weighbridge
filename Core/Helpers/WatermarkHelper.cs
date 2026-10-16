using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Weighbridge.Core.Helpers
{
    public static class WatermarkHelper
    {
        private const float DefaultOpacity = 0.07f;
        private const float DefaultSizeRatio = 0.55f;

        private static Image _logo;
        private static bool _logoLoaded;

        public static void Apply(Control surface, float opacity = DefaultOpacity, float sizeRatio = DefaultSizeRatio)
        {
            if (surface == null) return;
            Image logo = GetLogo();
            if (logo == null) return;

            EnableDoubleBuffer(surface);
            MakeChildrenTransparent(surface);

            int lastSide = -1;
            Bitmap current = null;

            void Render()
            {
                Size client = surface.ClientSize;
                int side = (int)(Math.Min(client.Width, client.Height) * sizeRatio);
                if (side < 40 || Math.Abs(side - lastSide) < 8) return;
                lastSide = side;

                Size target = FitInside(logo.Size, side);
                Bitmap next = RenderFaded(logo, target, opacity);
                surface.BackgroundImageLayout = ImageLayout.Center;
                surface.BackgroundImage = next;
                current?.Dispose();
                current = next;
            }

            surface.Resize += (s, e) => Render();
            surface.Disposed += (s, e) => current?.Dispose();
            Render();
        }

        public static void ApplyToGrid(DataGridView grid, float opacity = DefaultOpacity, float sizeRatio = DefaultSizeRatio)
        {
            if (grid == null) return;
            Image logo = GetLogo();
            if (logo == null) return;

            EnableDoubleBuffer(grid);

            int lastSide = -1;
            Bitmap current = null;

            void Render()
            {
                Size client = grid.ClientSize;
                int side = (int)(Math.Min(client.Width, client.Height) * sizeRatio);
                if (side < 40 || Math.Abs(side - lastSide) < 8) return;
                lastSide = side;

                Bitmap next = RenderFaded(logo, FitInside(logo.Size, side), opacity);
                current?.Dispose();
                current = next;
                grid.Invalidate();
            }

            grid.Paint += (s, e) =>
            {
                if (current == null) return;
                Size client = grid.ClientSize;
                e.Graphics.DrawImageUnscaled(current, (client.Width - current.Width) / 2, (client.Height - current.Height) / 2);
            };
            // Scroll menggeser piksel lama; gambar ulang agar logo tetap di tengah
            grid.Scroll += (s, e) => grid.Invalidate();
            grid.Resize += (s, e) => Render();
            grid.Disposed += (s, e) => current?.Dispose();
            Render();
        }

        public static Size FitInside(Size source, int maxSide)
        {
            if (source.Width <= 0 || source.Height <= 0 || maxSide <= 0) return Size.Empty;
            double scale = Math.Min((double)maxSide / source.Width, (double)maxSide / source.Height);
            return new Size(Math.Max(1, (int)Math.Round(source.Width * scale)), Math.Max(1, (int)Math.Round(source.Height * scale)));
        }

        private static Bitmap RenderFaded(Image source, Size target, float opacity)
        {
            var bmp = new Bitmap(target.Width, target.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            using (var attributes = new ImageAttributes())
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                attributes.SetColorMatrix(new ColorMatrix { Matrix33 = opacity });
                g.DrawImage(source, new Rectangle(Point.Empty, target), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        private static void MakeChildrenTransparent(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                // Hanya tipe bawaan; kontrol custom bisa tidak mendukung BackColor transparan
                Type t = c.GetType();
                bool supported = t == typeof(Label) || t == typeof(GroupBox) || t == typeof(Panel) ||
                                 t == typeof(CheckBox) || t == typeof(RadioButton);
                if (supported && c.BackColor == parent.BackColor)
                {
                    try { c.BackColor = Color.Transparent; } catch (ArgumentException) { }
                }

                if (t == typeof(GroupBox) || t == typeof(Panel))
                    MakeChildrenTransparent(c);
            }
        }

        private static void EnableDoubleBuffer(Control control)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(control, true, null);
            }
            catch { }
        }

        private static Image GetLogo()
        {
            if (_logoLoaded) return _logo;
            _logoLoaded = true;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "Assets", "logo-tanimas.png"),
                Path.Combine(baseDir, "..", "..", "Assets", "logo-tanimas.png")
            };

            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var img = Image.FromStream(stream))
                    {
                        _logo = new Bitmap(img);
                    }
                    break;
                }
                catch { }
            }
            return _logo;
        }
    }
}
