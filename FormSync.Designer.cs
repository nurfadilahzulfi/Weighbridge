using System.Drawing;
using System.Windows.Forms;

namespace Weighbridge
{
    partial class FormSync
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSync));
            this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.konfigurasiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.weighbridgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timbangMasukToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timbangKeluarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reprintToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateGRPODOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.timbangMasukUpdateTS = new System.Windows.Forms.ToolStripMenuItem();
            this.timbangKeluarUpdateTS = new System.Windows.Forms.ToolStripMenuItem();
            this.pbLogo = new System.Windows.Forms.PictureBox();
            this.lblWatermark = new System.Windows.Forms.Label();
            this.mainMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).BeginInit();
            this.SuspendLayout();
            this.mainMenuStrip.BackColor = System.Drawing.Color.White;
            this.mainMenuStrip.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.konfigurasiToolStripMenuItem,
            this.weighbridgeToolStripMenuItem,
            this.updateGRPODOToolStripMenuItem});
            this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.Padding = new System.Windows.Forms.Padding(6, 4, 0, 4);
            this.mainMenuStrip.Size = new System.Drawing.Size(884, 32);
            this.mainMenuStrip.TabIndex = 1;
            this.mainMenuStrip.Text = "menuStrip1";
            this.konfigurasiToolStripMenuItem.Name = "konfigurasiToolStripMenuItem";
            this.konfigurasiToolStripMenuItem.Size = new System.Drawing.Size(98, 24);
            this.konfigurasiToolStripMenuItem.Text = "Konfigurasi";
            this.konfigurasiToolStripMenuItem.Click += new System.EventHandler(this.konfigurasiToolStripMenuItem_Click);
            this.weighbridgeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timbangMasukToolStripMenuItem,
            this.timbangKeluarToolStripMenuItem,
            this.reprintToolStripMenuItem,
            this.reportToolStripMenuItem});
            this.weighbridgeToolStripMenuItem.Name = "weighbridgeToolStripMenuItem";
            this.weighbridgeToolStripMenuItem.Size = new System.Drawing.Size(109, 24);
            this.weighbridgeToolStripMenuItem.Text = "Weighbridge";
            this.timbangMasukToolStripMenuItem.Name = "timbangMasukToolStripMenuItem";
            this.timbangMasukToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.timbangMasukToolStripMenuItem.Text = "Timbang Masuk";
            this.timbangMasukToolStripMenuItem.Click += new System.EventHandler(this.timbangMasukToolStripMenuItem_Click);
            this.timbangKeluarToolStripMenuItem.Name = "timbangKeluarToolStripMenuItem";
            this.timbangKeluarToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.timbangKeluarToolStripMenuItem.Text = "Timbang Keluar";
            this.timbangKeluarToolStripMenuItem.Click += new System.EventHandler(this.timbangKeluarToolStripMenuItem_Click);
            this.reprintToolStripMenuItem.Name = "reprintToolStripMenuItem";
            this.reprintToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.reprintToolStripMenuItem.Text = "Reprint";
            this.reprintToolStripMenuItem.Click += new System.EventHandler(this.reprintToolStripMenuItem_Click);
            this.reportToolStripMenuItem.Name = "reportToolStripMenuItem";
            this.reportToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.reportToolStripMenuItem.Text = "Report";
            this.reportToolStripMenuItem.Click += new System.EventHandler(this.reportToolStripMenuItem_Click);
            this.updateGRPODOToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timbangMasukUpdateTS,
            this.timbangKeluarUpdateTS});
            this.updateGRPODOToolStripMenuItem.Name = "updateGRPODOToolStripMenuItem";
            this.updateGRPODOToolStripMenuItem.Size = new System.Drawing.Size(201, 24);
            this.updateGRPODOToolStripMenuItem.Text = "Update GRPO dan Delivery";
            this.timbangMasukUpdateTS.Name = "timbangMasukUpdateTS";
            this.timbangMasukUpdateTS.Size = new System.Drawing.Size(197, 26);
            this.timbangMasukUpdateTS.Text = "Timbang Masuk";
            this.timbangMasukUpdateTS.Click += new System.EventHandler(this.timbangMasukUpdateTS_Click);
            this.timbangKeluarUpdateTS.Name = "timbangKeluarUpdateTS";
            this.timbangKeluarUpdateTS.Size = new System.Drawing.Size(197, 26);
            this.timbangKeluarUpdateTS.Text = "Timbang Keluar";
            this.timbangKeluarUpdateTS.Click += new System.EventHandler(this.timbangKeluarUpdateTS_Click);
            this.pbLogo.BackColor = System.Drawing.Color.Transparent;
            this.pbLogo.Location = new System.Drawing.Point(302, 110);
            this.pbLogo.Name = "pbLogo";
            this.pbLogo.Size = new System.Drawing.Size(280, 280);
            this.pbLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbLogo.TabIndex = 2;
            this.pbLogo.TabStop = false;
            this.lblWatermark.BackColor = System.Drawing.Color.Transparent;
            this.lblWatermark.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblWatermark.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWatermark.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(116)))), ((int)(((byte)(139)))));
            this.lblWatermark.Location = new System.Drawing.Point(0, 516);
            this.lblWatermark.Name = "lblWatermark";
            this.lblWatermark.Size = new System.Drawing.Size(884, 36);
            this.lblWatermark.TabIndex = 3;
            this.lblWatermark.Text = "© 2026 Nur Fadilah Zulfi | Diah Fika Satrya • PT Tanimas Resources International";
            this.lblWatermark.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblWatermark.UseMnemonic = false; 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.ClientSize = new System.Drawing.Size(884, 552);
            this.MinimumSize = new System.Drawing.Size(650, 480);
            this.Controls.Add(this.pbLogo);
            this.Controls.Add(this.lblWatermark);
            this.Controls.Add(this.mainMenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Name = "FormSync";
            this.Text = "Weighbridge - PT Tanimas Resources International • © 2026 Nur Fadilah Zulfi | Diah Fika Satrya";
            this.Load += new System.EventHandler(this.FormSync_Load);
            this.Resize += new System.EventHandler(this.FormSync_Resize);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ToolStripMenuItem weighbridgeToolStripMenuItem;
        private ToolStripMenuItem timbangMasukToolStripMenuItem;
        private ToolStripMenuItem reprintToolStripMenuItem;
        private ToolStripMenuItem reportToolStripMenuItem;
        private ToolStripMenuItem konfigurasiToolStripMenuItem;
        private ToolStripMenuItem updateGRPODOToolStripMenuItem;
        protected MenuStrip mainMenuStrip;
        private ToolStripMenuItem timbangKeluarToolStripMenuItem;
        private ToolStripMenuItem timbangMasukUpdateTS;
        private ToolStripMenuItem timbangKeluarUpdateTS;
        private PictureBox pbLogo;
        private Label lblWatermark;
    }
}


