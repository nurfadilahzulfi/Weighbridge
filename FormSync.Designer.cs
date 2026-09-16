using System.Drawing;
using System.Windows.Forms;

namespace Weighbridge
{
    partial class FormSync
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
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
            this.mainMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenuStrip
            // 
            this.mainMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.konfigurasiToolStripMenuItem,
            this.weighbridgeToolStripMenuItem,
            this.updateGRPODOToolStripMenuItem});
            this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mainMenuStrip.Name = "mainMenuStrip";
            this.mainMenuStrip.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.mainMenuStrip.Size = new System.Drawing.Size(731, 28);
            this.mainMenuStrip.TabIndex = 1;
            this.mainMenuStrip.Text = "menuStrip1";
            // 
            // konfigurasiToolStripMenuItem
            // 
            this.konfigurasiToolStripMenuItem.Name = "konfigurasiToolStripMenuItem";
            this.konfigurasiToolStripMenuItem.Size = new System.Drawing.Size(98, 24);
            this.konfigurasiToolStripMenuItem.Text = "Konfigurasi";
            this.konfigurasiToolStripMenuItem.Click += new System.EventHandler(this.konfigurasiToolStripMenuItem_Click);
            // 
            // weighbridgeToolStripMenuItem
            // 
            this.weighbridgeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timbangMasukToolStripMenuItem,
            this.timbangKeluarToolStripMenuItem,
            this.reprintToolStripMenuItem,
            this.reportToolStripMenuItem});
            this.weighbridgeToolStripMenuItem.Name = "weighbridgeToolStripMenuItem";
            this.weighbridgeToolStripMenuItem.Size = new System.Drawing.Size(109, 24);
            this.weighbridgeToolStripMenuItem.Text = "Weighbridge";
            // 
            // timbangMasukToolStripMenuItem
            // 
            this.timbangMasukToolStripMenuItem.Name = "timbangMasukToolStripMenuItem";
            this.timbangMasukToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.timbangMasukToolStripMenuItem.Text = "Timbang Masuk";
            this.timbangMasukToolStripMenuItem.Click += new System.EventHandler(this.timbangMasukToolStripMenuItem_Click);
            // 
            // timbangKeluarToolStripMenuItem
            // 
            this.timbangKeluarToolStripMenuItem.Name = "timbangKeluarToolStripMenuItem";
            this.timbangKeluarToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.timbangKeluarToolStripMenuItem.Text = "Timbang Keluar";
            this.timbangKeluarToolStripMenuItem.Click += new System.EventHandler(this.timbangKeluarToolStripMenuItem_Click);
            // 
            // reprintToolStripMenuItem
            // 
            this.reprintToolStripMenuItem.Name = "reprintToolStripMenuItem";
            this.reprintToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.reprintToolStripMenuItem.Text = "Reprint";
            this.reprintToolStripMenuItem.Click += new System.EventHandler(this.reprintToolStripMenuItem_Click);
            // 
            // reportToolStripMenuItem
            // 
            this.reportToolStripMenuItem.Name = "reportToolStripMenuItem";
            this.reportToolStripMenuItem.Size = new System.Drawing.Size(197, 26);
            this.reportToolStripMenuItem.Text = "Report";
            this.reportToolStripMenuItem.Click += new System.EventHandler(this.reportToolStripMenuItem_Click);
            // 
            // updateGRPODOToolStripMenuItem
            // 
            this.updateGRPODOToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.timbangMasukUpdateTS,
            this.timbangKeluarUpdateTS});
            this.updateGRPODOToolStripMenuItem.Name = "updateGRPODOToolStripMenuItem";
            this.updateGRPODOToolStripMenuItem.Size = new System.Drawing.Size(201, 24);
            this.updateGRPODOToolStripMenuItem.Text = "Update GRPO dan Delivery";
            // 
            // timbangMasukUpdateTS
            // 
            this.timbangMasukUpdateTS.Name = "timbangMasukUpdateTS";
            this.timbangMasukUpdateTS.Size = new System.Drawing.Size(197, 26);
            this.timbangMasukUpdateTS.Text = "Timbang Masuk";
            this.timbangMasukUpdateTS.Click += new System.EventHandler(this.timbangMasukUpdateTS_Click);
            // 
            // timbangKeluarUpdateTS
            // 
            this.timbangKeluarUpdateTS.Name = "timbangKeluarUpdateTS";
            this.timbangKeluarUpdateTS.Size = new System.Drawing.Size(197, 26);
            this.timbangKeluarUpdateTS.Text = "Timbang Keluar";
            this.timbangKeluarUpdateTS.Click += new System.EventHandler(this.timbangKeluarUpdateTS_Click);
            // 
            // FormSync
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.ClientSize = new System.Drawing.Size(731, 198);
            this.Controls.Add(this.mainMenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormSync";
            this.Text = "Weighbridge - PT Tanimas Resources International";
            this.Load += new System.EventHandler(this.FormSync_Load);
            this.mainMenuStrip.ResumeLayout(false);
            this.mainMenuStrip.PerformLayout();
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
    }
}


