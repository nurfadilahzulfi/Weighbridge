
namespace Weighbridge
{
    partial class ConfigurationForm
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigurationForm));
            this.btnSvConfig = new System.Windows.Forms.Button();
            this.gbDB = new System.Windows.Forms.GroupBox();
            this.tbDBPass = new System.Windows.Forms.TextBox();
            this.tbDBUsrnm = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lbDBUsrnm = new System.Windows.Forms.Label();
            this.tbSBOPass = new System.Windows.Forms.TextBox();
            this.tbSBOUsrnm = new System.Windows.Forms.TextBox();
            this.tbDBSBO = new System.Windows.Forms.TextBox();
            this.tbDBSrv = new System.Windows.Forms.TextBox();
            this.cbSrvType = new System.Windows.Forms.ComboBox();
            this.lbDBPass = new System.Windows.Forms.Label();
            this.lbUsrnm = new System.Windows.Forms.Label();
            this.lbDB = new System.Windows.Forms.Label();
            this.lbServerNm = new System.Windows.Forms.Label();
            this.lbSvrType = new System.Windows.Forms.Label();
            this.btnTestWBConn = new System.Windows.Forms.Button();
            this.gbApp = new System.Windows.Forms.GroupBox();
            this.tbPrefixWB = new System.Windows.Forms.TextBox();
            this.lblPrefix = new System.Windows.Forms.Label();
            this.tbQtyTestTimbang = new System.Windows.Forms.TextBox();
            this.lbStatusWBConnection = new System.Windows.Forms.Label();
            this.tbWriteT = new System.Windows.Forms.TextBox();
            this.tbReadT = new System.Windows.Forms.TextBox();
            this.tbDataBits = new System.Windows.Forms.TextBox();
            this.tbBaudRate = new System.Windows.Forms.TextBox();
            this.cbPort = new System.Windows.Forms.ComboBox();
            this.lbWriteT = new System.Windows.Forms.Label();
            this.lbReadT = new System.Windows.Forms.Label();
            this.lbDataBits = new System.Windows.Forms.Label();
            this.lbBaudRate = new System.Windows.Forms.Label();
            this.lbPort = new System.Windows.Forms.Label();
            this.gbDB.SuspendLayout();
            this.gbApp.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSvConfig
            // 
            this.btnSvConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSvConfig.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSvConfig.Location = new System.Drawing.Point(638, 296);
            this.btnSvConfig.Name = "btnSvConfig";
            this.btnSvConfig.Size = new System.Drawing.Size(84, 24);
            this.btnSvConfig.TabIndex = 13;
            this.btnSvConfig.Text = "Save";
            this.btnSvConfig.UseVisualStyleBackColor = true;
            this.btnSvConfig.Click += new System.EventHandler(this.btnSvConfig_Click);
            // 
            // gbDB
            // 
            this.gbDB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDB.Controls.Add(this.tbDBPass);
            this.gbDB.Controls.Add(this.tbDBUsrnm);
            this.gbDB.Controls.Add(this.label1);
            this.gbDB.Controls.Add(this.lbDBUsrnm);
            this.gbDB.Controls.Add(this.tbSBOPass);
            this.gbDB.Controls.Add(this.tbSBOUsrnm);
            this.gbDB.Controls.Add(this.tbDBSBO);
            this.gbDB.Controls.Add(this.tbDBSrv);
            this.gbDB.Controls.Add(this.cbSrvType);
            this.gbDB.Controls.Add(this.lbDBPass);
            this.gbDB.Controls.Add(this.lbUsrnm);
            this.gbDB.Controls.Add(this.lbDB);
            this.gbDB.Controls.Add(this.lbServerNm);
            this.gbDB.Controls.Add(this.lbSvrType);
            this.gbDB.Location = new System.Drawing.Point(366, 20);
            this.gbDB.Name = "gbDB";
            this.gbDB.Size = new System.Drawing.Size(356, 269);
            this.gbDB.TabIndex = 12;
            this.gbDB.TabStop = false;
            this.gbDB.Text = "SBO Database";
            // 
            // tbDBPass
            // 
            this.tbDBPass.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDBPass.Location = new System.Drawing.Point(116, 124);
            this.tbDBPass.Name = "tbDBPass";
            this.tbDBPass.Size = new System.Drawing.Size(239, 20);
            this.tbDBPass.TabIndex = 15;
            // 
            // tbDBUsrnm
            // 
            this.tbDBUsrnm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDBUsrnm.Location = new System.Drawing.Point(116, 99);
            this.tbDBUsrnm.Name = "tbDBUsrnm";
            this.tbDBUsrnm.Size = new System.Drawing.Size(239, 20);
            this.tbDBUsrnm.TabIndex = 14;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 15);
            this.label1.TabIndex = 13;
            this.label1.Text = "Database Password";
            // 
            // lbDBUsrnm
            // 
            this.lbDBUsrnm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDBUsrnm.AutoSize = true;
            this.lbDBUsrnm.Location = new System.Drawing.Point(5, 102);
            this.lbDBUsrnm.Name = "lbDBUsrnm";
            this.lbDBUsrnm.Size = new System.Drawing.Size(121, 15);
            this.lbDBUsrnm.TabIndex = 12;
            this.lbDBUsrnm.Text = "Database Username";
            // 
            // tbSBOPass
            // 
            this.tbSBOPass.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSBOPass.Location = new System.Drawing.Point(116, 175);
            this.tbSBOPass.Name = "tbSBOPass";
            this.tbSBOPass.Size = new System.Drawing.Size(239, 20);
            this.tbSBOPass.TabIndex = 11;
            // 
            // tbSBOUsrnm
            // 
            this.tbSBOUsrnm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSBOUsrnm.Location = new System.Drawing.Point(116, 150);
            this.tbSBOUsrnm.Name = "tbSBOUsrnm";
            this.tbSBOUsrnm.Size = new System.Drawing.Size(239, 20);
            this.tbSBOUsrnm.TabIndex = 10;
            // 
            // tbDBSBO
            // 
            this.tbDBSBO.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDBSBO.Location = new System.Drawing.Point(116, 73);
            this.tbDBSBO.Name = "tbDBSBO";
            this.tbDBSBO.Size = new System.Drawing.Size(239, 20);
            this.tbDBSBO.TabIndex = 9;
            // 
            // tbDBSrv
            // 
            this.tbDBSrv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDBSrv.Location = new System.Drawing.Point(116, 48);
            this.tbDBSrv.Name = "tbDBSrv";
            this.tbDBSrv.Size = new System.Drawing.Size(239, 20);
            this.tbDBSrv.TabIndex = 7;
            // 
            // cbSrvType
            // 
            this.cbSrvType.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbSrvType.FormattingEnabled = true;
            this.cbSrvType.Items.AddRange(new object[] {
            "MSSQL2012",
            "MSSQL2014",
            "MSSQL2016",
            "MSSQL2017",
            "MSSQL2019",
            "HANA"});
            this.cbSrvType.Location = new System.Drawing.Point(116, 23);
            this.cbSrvType.Name = "cbSrvType";
            this.cbSrvType.Size = new System.Drawing.Size(239, 21);
            this.cbSrvType.TabIndex = 6;
            // 
            // lbDBPass
            // 
            this.lbDBPass.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDBPass.AutoSize = true;
            this.lbDBPass.Location = new System.Drawing.Point(5, 177);
            this.lbDBPass.Name = "lbDBPass";
            this.lbDBPass.Size = new System.Drawing.Size(89, 15);
            this.lbDBPass.TabIndex = 5;
            this.lbDBPass.Text = "SBO Password";
            // 
            // lbUsrnm
            // 
            this.lbUsrnm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbUsrnm.AutoSize = true;
            this.lbUsrnm.Location = new System.Drawing.Point(5, 153);
            this.lbUsrnm.Name = "lbUsrnm";
            this.lbUsrnm.Size = new System.Drawing.Size(93, 15);
            this.lbUsrnm.TabIndex = 4;
            this.lbUsrnm.Text = "SBO Username";
            // 
            // lbDB
            // 
            this.lbDB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDB.AutoSize = true;
            this.lbDB.Location = new System.Drawing.Point(5, 76);
            this.lbDB.Name = "lbDB";
            this.lbDB.Size = new System.Drawing.Size(60, 15);
            this.lbDB.TabIndex = 3;
            this.lbDB.Text = "Database";
            // 
            // lbServerNm
            // 
            this.lbServerNm.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbServerNm.AutoSize = true;
            this.lbServerNm.Location = new System.Drawing.Point(5, 50);
            this.lbServerNm.Name = "lbServerNm";
            this.lbServerNm.Size = new System.Drawing.Size(42, 15);
            this.lbServerNm.TabIndex = 1;
            this.lbServerNm.Text = "Server";
            // 
            // lbSvrType
            // 
            this.lbSvrType.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbSvrType.AutoSize = true;
            this.lbSvrType.Location = new System.Drawing.Point(5, 26);
            this.lbSvrType.Name = "lbSvrType";
            this.lbSvrType.Size = new System.Drawing.Size(71, 15);
            this.lbSvrType.TabIndex = 0;
            this.lbSvrType.Text = "Server Type";
            // 
            // btnTestWBConn
            // 
            this.btnTestWBConn.Location = new System.Drawing.Point(221, 230);
            this.btnTestWBConn.Name = "btnTestWBConn";
            this.btnTestWBConn.Size = new System.Drawing.Size(116, 23);
            this.btnTestWBConn.TabIndex = 14;
            this.btnTestWBConn.Text = "Test WB Connection";
            this.btnTestWBConn.UseVisualStyleBackColor = true;
            this.btnTestWBConn.Click += new System.EventHandler(this.btnTestWBConn_Click);
            // 
            // gbApp
            // 
            this.gbApp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbApp.Controls.Add(this.tbPrefixWB);
            this.gbApp.Controls.Add(this.lblPrefix);
            this.gbApp.Controls.Add(this.btnTestWBConn);
            this.gbApp.Controls.Add(this.tbQtyTestTimbang);
            this.gbApp.Controls.Add(this.lbStatusWBConnection);
            this.gbApp.Controls.Add(this.tbWriteT);
            this.gbApp.Controls.Add(this.tbReadT);
            this.gbApp.Controls.Add(this.tbDataBits);
            this.gbApp.Controls.Add(this.tbBaudRate);
            this.gbApp.Controls.Add(this.cbPort);
            this.gbApp.Controls.Add(this.lbWriteT);
            this.gbApp.Controls.Add(this.lbReadT);
            this.gbApp.Controls.Add(this.lbDataBits);
            this.gbApp.Controls.Add(this.lbBaudRate);
            this.gbApp.Controls.Add(this.lbPort);
            this.gbApp.Location = new System.Drawing.Point(13, 20);
            this.gbApp.Name = "gbApp";
            this.gbApp.Size = new System.Drawing.Size(352, 269);
            this.gbApp.TabIndex = 11;
            this.gbApp.TabStop = false;
            this.gbApp.Text = "Weighbridge";
            // 
            // tbPrefixWB
            // 
            this.tbPrefixWB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbPrefixWB.Location = new System.Drawing.Point(116, 146);
            this.tbPrefixWB.Name = "tbPrefixWB";
            this.tbPrefixWB.Size = new System.Drawing.Size(223, 20);
            this.tbPrefixWB.TabIndex = 15;
            // 
            // lblPrefix
            // 
            this.lblPrefix.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblPrefix.AutoSize = true;
            this.lblPrefix.Location = new System.Drawing.Point(5, 149);
            this.lblPrefix.Name = "lblPrefix";
            this.lblPrefix.Size = new System.Drawing.Size(38, 15);
            this.lblPrefix.TabIndex = 14;
            this.lblPrefix.Text = "Prefix";
            // 
            // tbQtyTestTimbang
            // 
            this.tbQtyTestTimbang.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbQtyTestTimbang.Location = new System.Drawing.Point(116, 197);
            this.tbQtyTestTimbang.Name = "tbQtyTestTimbang";
            this.tbQtyTestTimbang.Size = new System.Drawing.Size(223, 20);
            this.tbQtyTestTimbang.TabIndex = 13;
            // 
            // lbStatusWBConnection
            // 
            this.lbStatusWBConnection.AutoSize = true;
            this.lbStatusWBConnection.Location = new System.Drawing.Point(243, 175);
            this.lbStatusWBConnection.Name = "lbStatusWBConnection";
            this.lbStatusWBConnection.Size = new System.Drawing.Size(106, 15);
            this.lbStatusWBConnection.TabIndex = 12;
            this.lbStatusWBConnection.Text = "Connection Status";
            // 
            // tbWriteT
            // 
            this.tbWriteT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbWriteT.Location = new System.Drawing.Point(116, 120);
            this.tbWriteT.Name = "tbWriteT";
            this.tbWriteT.Size = new System.Drawing.Size(223, 20);
            this.tbWriteT.TabIndex = 9;
            // 
            // tbReadT
            // 
            this.tbReadT.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbReadT.Location = new System.Drawing.Point(116, 96);
            this.tbReadT.Name = "tbReadT";
            this.tbReadT.Size = new System.Drawing.Size(223, 20);
            this.tbReadT.TabIndex = 8;
            // 
            // tbDataBits
            // 
            this.tbDataBits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbDataBits.Location = new System.Drawing.Point(115, 72);
            this.tbDataBits.Name = "tbDataBits";
            this.tbDataBits.Size = new System.Drawing.Size(224, 20);
            this.tbDataBits.TabIndex = 7;
            // 
            // tbBaudRate
            // 
            this.tbBaudRate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tbBaudRate.Location = new System.Drawing.Point(115, 48);
            this.tbBaudRate.Name = "tbBaudRate";
            this.tbBaudRate.Size = new System.Drawing.Size(224, 20);
            this.tbBaudRate.TabIndex = 6;
            // 
            // cbPort
            // 
            this.cbPort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.cbPort.FormattingEnabled = true;
            this.cbPort.Items.AddRange(new object[] {
            "COM1",
            "COM3",
            "COM4",
            "COM5",
            "COM6"});
            this.cbPort.Location = new System.Drawing.Point(115, 23);
            this.cbPort.Name = "cbPort";
            this.cbPort.Size = new System.Drawing.Size(224, 21);
            this.cbPort.TabIndex = 5;
            // 
            // lbWriteT
            // 
            this.lbWriteT.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbWriteT.AutoSize = true;
            this.lbWriteT.Location = new System.Drawing.Point(5, 123);
            this.lbWriteT.Name = "lbWriteT";
            this.lbWriteT.Size = new System.Drawing.Size(83, 15);
            this.lbWriteT.TabIndex = 4;
            this.lbWriteT.Text = "Write Timeout";
            // 
            // lbReadT
            // 
            this.lbReadT.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbReadT.AutoSize = true;
            this.lbReadT.Location = new System.Drawing.Point(5, 99);
            this.lbReadT.Name = "lbReadT";
            this.lbReadT.Size = new System.Drawing.Size(85, 15);
            this.lbReadT.TabIndex = 3;
            this.lbReadT.Text = "Read Timeout";
            // 
            // lbDataBits
            // 
            this.lbDataBits.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDataBits.AutoSize = true;
            this.lbDataBits.Location = new System.Drawing.Point(5, 75);
            this.lbDataBits.Name = "lbDataBits";
            this.lbDataBits.Size = new System.Drawing.Size(56, 15);
            this.lbDataBits.TabIndex = 2;
            this.lbDataBits.Text = "Data Bits";
            // 
            // lbBaudRate
            // 
            this.lbBaudRate.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbBaudRate.AutoSize = true;
            this.lbBaudRate.Location = new System.Drawing.Point(5, 50);
            this.lbBaudRate.Name = "lbBaudRate";
            this.lbBaudRate.Size = new System.Drawing.Size(65, 15);
            this.lbBaudRate.TabIndex = 1;
            this.lbBaudRate.Text = "Baud Rate";
            // 
            // lbPort
            // 
            this.lbPort.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbPort.AutoSize = true;
            this.lbPort.Location = new System.Drawing.Point(5, 26);
            this.lbPort.Name = "lbPort";
            this.lbPort.Size = new System.Drawing.Size(66, 15);
            this.lbPort.TabIndex = 0;
            this.lbPort.Text = "Port Name";
            // 
            // ConfigurationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(735, 336);
            this.Controls.Add(this.btnSvConfig);
            this.Controls.Add(this.gbDB);
            this.Controls.Add(this.gbApp);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.Text = "Konfigurasi - PT Tanimas Resources International";
            this.Load += new System.EventHandler(this.ConfigurationForm_Load);
            this.gbDB.ResumeLayout(false);
            this.gbDB.PerformLayout();
            this.gbApp.ResumeLayout(false);
            this.gbApp.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnSvConfig;
        private System.Windows.Forms.GroupBox gbDB;
        private System.Windows.Forms.TextBox tbDBPass;
        private System.Windows.Forms.TextBox tbDBUsrnm;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lbDBUsrnm;
        private System.Windows.Forms.TextBox tbSBOPass;
        private System.Windows.Forms.TextBox tbSBOUsrnm;
        private System.Windows.Forms.TextBox tbDBSBO;
        private System.Windows.Forms.TextBox tbDBSrv;
        private System.Windows.Forms.ComboBox cbSrvType;
        private System.Windows.Forms.Label lbDBPass;
        private System.Windows.Forms.Label lbUsrnm;
        private System.Windows.Forms.Label lbDB;
        private System.Windows.Forms.Label lbServerNm;
        private System.Windows.Forms.Label lbSvrType;
        private System.Windows.Forms.Button btnTestWBConn;
        private System.Windows.Forms.GroupBox gbApp;
        private System.Windows.Forms.TextBox tbPrefixWB;
        private System.Windows.Forms.Label lblPrefix;
        private System.Windows.Forms.TextBox tbQtyTestTimbang;
        private System.Windows.Forms.Label lbStatusWBConnection;
        private System.Windows.Forms.TextBox tbWriteT;
        private System.Windows.Forms.TextBox tbReadT;
        private System.Windows.Forms.TextBox tbDataBits;
        private System.Windows.Forms.TextBox tbBaudRate;
        private System.Windows.Forms.ComboBox cbPort;
        private System.Windows.Forms.Label lbWriteT;
        private System.Windows.Forms.Label lbReadT;
        private System.Windows.Forms.Label lbDataBits;
        private System.Windows.Forms.Label lbBaudRate;
        private System.Windows.Forms.Label lbPort;
    }
}