using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Weighbridge
{
    public class WeighStatusForm : Form
    {
        private Label lblMessage;
        private PictureBox picIcon;
        private System.Windows.Forms.Timer _autoCloseTimer;

        public WeighStatusForm()
        {
            InitializeFormDialogStatus();
        }

        private void InitializeFormDialogStatus()
        {
            // Form settings
            this.Text = "Weighbridge";
            this.Size = new Size(320, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ControlBox = false;   // hide X button
            this.TopMost = true;

            // Icon label (emoji as icon)
            picIcon = new PictureBox()
            {
                Text = "⚖️",
                Font = new Font("Segoe UI", 24),
                Size = new Size(50, 50),
                Location = new Point(20, 40),
                AutoSize = false,
            };
            // Use label instead for emoji
            var lblIcon = new Label()
            {
                Text = "⚖️",
                Font = new Font("Segoe UI", 24),
                Size = new Size(55, 50),
                Location = new Point(15, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Message label
            lblMessage = new Label()
            {
                Text = "Reading weight...",
                Font = new Font("Segoe UI", 11),
                Size = new Size(220, 50),
                Location = new Point(80, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };

            this.Controls.Add(lblIcon);
            this.Controls.Add(lblMessage);
        }

        // Call this to update message and auto-close
        public void ShowSuccess(string weight, int closeDelayMs = 1500)
        {
            lblMessage.Text = $"✅ Weight captured!\n{weight} KG";
            lblMessage.ForeColor = Color.Green;
            lblMessage.Font = new Font("Segoe UI", 11, FontStyle.Bold);

            _autoCloseTimer = new System.Windows.Forms.Timer();
            _autoCloseTimer.Interval = closeDelayMs;
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                this.Close();
            };
            _autoCloseTimer.Start();
        }

        public void ShowReading()
        {
            lblMessage.Text = "⏳ Reading weight\nPlease wait...";
            lblMessage.ForeColor = Color.Black;
            lblMessage.Font = new Font("Segoe UI", 11);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WeighStatusForm));
            this.SuspendLayout();
            // 
            // WeighStatusForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "WeighStatusForm";
            this.ResumeLayout(false);

        }
    }
}
