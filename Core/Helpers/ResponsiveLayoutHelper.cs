using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Weighbridge.Core.Helpers
{
    public sealed class ResponsiveLayoutHelper
    {
        private sealed class ControlState
        {
            public Control Control;
            public Rectangle Bounds;
            public Font Font;
        }

        private readonly Form _form;
        private readonly Size _baseClientSize;
        private readonly List<ControlState> _states = new List<ControlState>();
        private readonly List<Font> _createdFonts = new List<Font>();
        private float _lastFontScale = 1f;

        private ResponsiveLayoutHelper(Form form)
        {
            _form = form;
            _baseClientSize = form.ClientSize;
            Capture(form.Controls);

            _form.Resize += (s, e) => Apply();
            _form.Disposed += (s, e) => DisposeFonts(_createdFonts);
        }

        public static ResponsiveLayoutHelper Attach(Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            var helper = new ResponsiveLayoutHelper(form);
            helper.Apply();
            return helper;
        }

        public static SizeF ComputeScale(Size baseSize, Size currentSize, out float fontScale)
        {
            float sx = baseSize.Width > 0 ? Math.Max(1f, (float)currentSize.Width / baseSize.Width) : 1f;
            float sy = baseSize.Height > 0 ? Math.Max(1f, (float)currentSize.Height / baseSize.Height) : 1f;
            fontScale = (float)(Math.Round(Math.Min(sx, sy) * 20f) / 20f);
            return new SizeF(sx, sy);
        }

        public static Rectangle ScaleBounds(Rectangle original, SizeF scale)
        {
            int left = (int)Math.Round(original.Left * scale.Width);
            int top = (int)Math.Round(original.Top * scale.Height);
            int right = (int)Math.Round(original.Right * scale.Width);
            int bottom = (int)Math.Round(original.Bottom * scale.Height);
            return new Rectangle(left, top, right - left, bottom - top);
        }

        private void Capture(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                _states.Add(new ControlState { Control = c, Bounds = c.Bounds, Font = c.Font });
                Capture(c.Controls);
            }
        }

        private void Apply()
        {
            if (_form.WindowState == FormWindowState.Minimized) return;

            float fontScale;
            SizeF scale = ComputeScale(_baseClientSize, _form.ClientSize, out fontScale);
            bool fontChanged = Math.Abs(fontScale - _lastFontScale) > 0.001f;
            var oldFonts = fontChanged ? new List<Font>(_createdFonts) : null;
            if (fontChanged) _createdFonts.Clear();

            Point scroll = _form.AutoScrollPosition;
            _form.SuspendLayout();
            try
            {
                foreach (var st in _states)
                {
                    if (fontChanged)
                    {
                        Font f = st.Font;
                        if (Math.Abs(fontScale - 1f) > 0.001f)
                        {
                            f = new Font(st.Font.FontFamily, st.Font.Size * fontScale, st.Font.Style, st.Font.Unit, st.Font.GdiCharSet);
                            _createdFonts.Add(f);
                        }
                        st.Control.Font = f;
                    }

                    Rectangle b = ScaleBounds(st.Bounds, scale);
                    if (st.Control.Parent == _form) b.Offset(scroll);
                    st.Control.Bounds = b;
                }
            }
            finally
            {
                _form.ResumeLayout(true);
            }

            _lastFontScale = fontScale;
            if (oldFonts != null) DisposeFonts(oldFonts);
        }

        private static void DisposeFonts(List<Font> fonts)
        {
            foreach (var f in fonts) f.Dispose();
            fonts.Clear();
        }
    }
}
