
namespace Weighbridge
{
    partial class ReportForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ReportForm));
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnSearchReport = new System.Windows.Forms.Button();
            this.cmbCommodity = new System.Windows.Forms.ComboBox();
            this.lblCommodityReport = new System.Windows.Forms.Label();
            this.dtTo = new System.Windows.Forms.DateTimePicker();
            this.dtFrom = new System.Windows.Forms.DateTimePicker();
            this.lblFromDate = new System.Windows.Forms.Label();
            this.dvgReportData = new System.Windows.Forms.DataGridView();
            this.lblNoTiket = new System.Windows.Forms.Label();
            this.lblNopol = new System.Windows.Forms.Label();
            this.tbNoTiket = new System.Windows.Forms.TextBox();
            this.tbNopol = new System.Windows.Forms.TextBox();
            this.lblToDate = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dvgReportData)).BeginInit();
            this.SuspendLayout();
            // 
            // btnExportExcel
            //
            this.btnExportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportExcel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportExcel.Location = new System.Drawing.Point(582, 515);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(114, 31);
            this.btnExportExcel.TabIndex = 17;
            this.btnExportExcel.Text = "Export to Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnSearchReport
            // 
            this.btnSearchReport.Location = new System.Drawing.Point(597, 81);
            this.btnSearchReport.Name = "btnSearchReport";
            this.btnSearchReport.Size = new System.Drawing.Size(75, 23);
            this.btnSearchReport.TabIndex = 16;
            this.btnSearchReport.Text = "Search";
            this.btnSearchReport.UseVisualStyleBackColor = true;
            this.btnSearchReport.Click += new System.EventHandler(this.btnSearchReport_Click);
            // 
            // cmbCommodity
            // 
            this.cmbCommodity.FormattingEnabled = true;
            this.cmbCommodity.Location = new System.Drawing.Point(121, 78);
            this.cmbCommodity.Name = "cmbCommodity";
            this.cmbCommodity.Size = new System.Drawing.Size(208, 21);
            this.cmbCommodity.TabIndex = 15;
            // 
            // lblCommodityReport
            // 
            this.lblCommodityReport.AutoSize = true;
            this.lblCommodityReport.Location = new System.Drawing.Point(33, 81);
            this.lblCommodityReport.Name = "lblCommodityReport";
            this.lblCommodityReport.Size = new System.Drawing.Size(69, 15);
            this.lblCommodityReport.TabIndex = 14;
            this.lblCommodityReport.Text = "Commodity";
            // 
            // dtTo
            // 
            this.dtTo.Location = new System.Drawing.Point(473, 48);
            this.dtTo.Name = "dtTo";
            this.dtTo.Size = new System.Drawing.Size(200, 20);
            this.dtTo.TabIndex = 13;
            // 
            // dtFrom
            // 
            this.dtFrom.Location = new System.Drawing.Point(472, 18);
            this.dtFrom.Name = "dtFrom";
            this.dtFrom.Size = new System.Drawing.Size(200, 20);
            this.dtFrom.TabIndex = 12;
            // 
            // lblFromDate
            // 
            this.lblFromDate.AutoSize = true;
            this.lblFromDate.Location = new System.Drawing.Point(403, 19);
            this.lblFromDate.Name = "lblFromDate";
            this.lblFromDate.Size = new System.Drawing.Size(65, 15);
            this.lblFromDate.TabIndex = 10;
            this.lblFromDate.Text = "From Date";
            // 
            // dvgReportData
            // 
            this.dvgReportData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dvgReportData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dvgReportData.Location = new System.Drawing.Point(12, 129);
            this.dvgReportData.Name = "dvgReportData";
            this.dvgReportData.RowHeadersWidth = 51;
            this.dvgReportData.Size = new System.Drawing.Size(684, 364);
            this.dvgReportData.TabIndex = 9;
            // 
            // lblNoTiket
            // 
            this.lblNoTiket.AutoSize = true;
            this.lblNoTiket.Location = new System.Drawing.Point(33, 21);
            this.lblNoTiket.Name = "lblNoTiket";
            this.lblNoTiket.Size = new System.Drawing.Size(74, 15);
            this.lblNoTiket.TabIndex = 18;
            this.lblNoTiket.Text = "Nomor Tiket";
            // 
            // lblNopol
            // 
            this.lblNopol.AutoSize = true;
            this.lblNopol.Location = new System.Drawing.Point(33, 52);
            this.lblNopol.Name = "lblNopol";
            this.lblNopol.Size = new System.Drawing.Size(78, 15);
            this.lblNopol.TabIndex = 19;
            this.lblNopol.Text = "Nomor Polisi";
            // 
            // tbNoTiket
            // 
            this.tbNoTiket.Location = new System.Drawing.Point(121, 18);
            this.tbNoTiket.Name = "tbNoTiket";
            this.tbNoTiket.Size = new System.Drawing.Size(208, 20);
            this.tbNoTiket.TabIndex = 20;
            // 
            // tbNopol
            // 
            this.tbNopol.Location = new System.Drawing.Point(121, 47);
            this.tbNopol.Name = "tbNopol";
            this.tbNopol.Size = new System.Drawing.Size(208, 20);
            this.tbNopol.TabIndex = 21;
            // 
            // lblToDate
            // 
            this.lblToDate.AutoSize = true;
            this.lblToDate.Location = new System.Drawing.Point(406, 52);
            this.lblToDate.Name = "lblToDate";
            this.lblToDate.Size = new System.Drawing.Size(50, 15);
            this.lblToDate.TabIndex = 11;
            this.lblToDate.Text = "To Date";
            // 
            // ReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(713, 559);
            this.Controls.Add(this.tbNopol);
            this.Controls.Add(this.tbNoTiket);
            this.Controls.Add(this.lblNopol);
            this.Controls.Add(this.lblNoTiket);
            this.Controls.Add(this.btnExportExcel);
            this.Controls.Add(this.btnSearchReport);
            this.Controls.Add(this.cmbCommodity);
            this.Controls.Add(this.lblCommodityReport);
            this.Controls.Add(this.dtTo);
            this.Controls.Add(this.dtFrom);
            this.Controls.Add(this.lblToDate);
            this.Controls.Add(this.lblFromDate);
            this.Controls.Add(this.dvgReportData);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Name = "ReportForm";
            this.Text = "Report Weighbridge - PT Tanimas Resources International • © 2026 Nur Fadilah Zulfi | Diah Fika Satrya";
            this.Load += new System.EventHandler(this.ReportForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dvgReportData)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnSearchReport;
        private System.Windows.Forms.ComboBox cmbCommodity;
        private System.Windows.Forms.Label lblCommodityReport;
        private System.Windows.Forms.DateTimePicker dtTo;
        private System.Windows.Forms.DateTimePicker dtFrom;
        private System.Windows.Forms.Label lblFromDate;
        private System.Windows.Forms.DataGridView dvgReportData;
        private System.Windows.Forms.Label lblNoTiket;
        private System.Windows.Forms.Label lblNopol;
        private System.Windows.Forms.TextBox tbNoTiket;
        private System.Windows.Forms.TextBox tbNopol;
        private System.Windows.Forms.Label lblToDate;
    }
}