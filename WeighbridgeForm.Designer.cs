
namespace Weighbridge
{
    partial class WeighbridgeForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WeighbridgeForm));
            this.tbCommodity = new System.Windows.Forms.TextBox();
            this.lblCommodity = new System.Windows.Forms.Label();
            this.lblUsername = new System.Windows.Forms.Label();
            this.tbContNo2 = new System.Windows.Forms.TextBox();
            this.lblContNo2 = new System.Windows.Forms.Label();
            this.tbContNo1 = new System.Windows.Forms.TextBox();
            this.lblContNo1 = new System.Windows.Forms.Label();
            this.tbNote = new System.Windows.Forms.TextBox();
            this.lblNote = new System.Windows.Forms.Label();
            this.tbDONo = new System.Windows.Forms.TextBox();
            this.lblDONo = new System.Windows.Forms.Label();
            this.tbSealNo = new System.Windows.Forms.TextBox();
            this.lblSealNo = new System.Windows.Forms.Label();
            this.tbJnsTruck = new System.Windows.Forms.TextBox();
            this.lbJnsTruck = new System.Windows.Forms.Label();
            this.lbDateTime = new System.Windows.Forms.Label();
            this.gbWINKen = new System.Windows.Forms.GroupBox();
            this.tbBaseSAP = new System.Windows.Forms.TextBox();
            this.tbMOISTKebun = new System.Windows.Forms.TextBox();
            this.tbFFAKebun = new System.Windows.Forms.TextBox();
            this.lbMOISTKebun = new System.Windows.Forms.Label();
            this.lbFFAKebun = new System.Windows.Forms.Label();
            this.tbNettoKebun = new System.Windows.Forms.TextBox();
            this.lblNettoKebun = new System.Windows.Forms.Label();
            this.tbTaraKebun = new System.Windows.Forms.TextBox();
            this.lblTaraKebun = new System.Windows.Forms.Label();
            this.tbBrutoKebun = new System.Windows.Forms.TextBox();
            this.lblBrutoKebun = new System.Windows.Forms.Label();
            this.gbUjiLab = new System.Windows.Forms.GroupBox();
            this.tbDOBI = new System.Windows.Forms.TextBox();
            this.tbIMP = new System.Windows.Forms.TextBox();
            this.tbMOIST = new System.Windows.Forms.TextBox();
            this.tbCAROTINE = new System.Windows.Forms.TextBox();
            this.lblCAROTINE = new System.Windows.Forms.Label();
            this.lblDOBI = new System.Windows.Forms.Label();
            this.lblIMP = new System.Windows.Forms.Label();
            this.lblMoist = new System.Windows.Forms.Label();
            this.tbFFA = new System.Windows.Forms.TextBox();
            this.lblFFA = new System.Windows.Forms.Label();
            this.cbTMManual = new System.Windows.Forms.CheckBox();
            this.btnTimbangMasuk = new System.Windows.Forms.Button();
            this.tbQtyMasuk = new System.Windows.Forms.TextBox();
            this.btnWBSave = new System.Windows.Forms.Button();
            this.tbNopol = new System.Windows.Forms.TextBox();
            this.tbSIMSupir = new System.Windows.Forms.TextBox();
            this.lbWINNopol = new System.Windows.Forms.Label();
            this.lbWINSupir = new System.Windows.Forms.Label();
            this.tbNamaSupir = new System.Windows.Forms.TextBox();
            this.lbWINEksCod = new System.Windows.Forms.Label();
            this.runningTime = new System.Windows.Forms.Timer(this.components);
            this.tbTransporterEks = new System.Windows.Forms.TextBox();
            this.lblTransporterEks = new System.Windows.Forms.Label();
            this.lblTransporterInt = new System.Windows.Forms.Label();
            this.gbPabean = new System.Windows.Forms.GroupBox();
            this.tbNomorAju = new System.Windows.Forms.TextBox();
            this.lblNomorAju = new System.Windows.Forms.Label();
            this.tbNomorSPB = new System.Windows.Forms.TextBox();
            this.lblNomorSPB = new System.Windows.Forms.Label();
            this.dtTglSPB = new System.Windows.Forms.DateTimePicker();
            this.lbTglSPB = new System.Windows.Forms.Label();
            this.cbStatusJalur = new System.Windows.Forms.ComboBox();
            this.lbStatusJalur = new System.Windows.Forms.Label();
            this.cbFasilitasPabean = new System.Windows.Forms.ComboBox();
            this.lbFasilitasPabean = new System.Windows.Forms.Label();
            this.cmbKodeDokPabean = new System.Windows.Forms.ComboBox();
            this.dtTglDokPabean = new System.Windows.Forms.DateTimePicker();
            this.lbTglDokPabean = new System.Windows.Forms.Label();
            this.tbNoDokPabean = new System.Windows.Forms.TextBox();
            this.lbNoDokPabean = new System.Windows.Forms.Label();
            this.lbKodeDokPabean = new System.Windows.Forms.Label();
            this.lblValLineNumSAP = new System.Windows.Forms.Label();
            this.btnRefreshData = new System.Windows.Forms.Button();
            this.lbValDocEntSAP = new System.Windows.Forms.Label();
            this.cbDocType = new System.Windows.Forms.ComboBox();
            this.btnCariBaseDoc = new System.Windows.Forms.Button();
            this.lbTiketNum = new System.Windows.Forms.Label();
            this.tbTiketNum = new System.Windows.Forms.TextBox();
            this.lbWINBaseDoc = new System.Windows.Forms.Label();
            this.cbTransInt = new System.Windows.Forms.ComboBox();
            this.tbBaseDoc = new System.Windows.Forms.TextBox();
            this.gbWINKen.SuspendLayout();
            this.gbUjiLab.SuspendLayout();
            this.gbPabean.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbCommodity
            // 
            this.tbCommodity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbCommodity.Enabled = false;
            this.tbCommodity.Location = new System.Drawing.Point(544, 42);
            this.tbCommodity.Name = "tbCommodity";
            this.tbCommodity.Size = new System.Drawing.Size(242, 20);
            this.tbCommodity.TabIndex = 103;
            // 
            // lblCommodity
            // 
            this.lblCommodity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCommodity.AutoSize = true;
            this.lblCommodity.Location = new System.Drawing.Point(424, 43);
            this.lblCommodity.Name = "lblCommodity";
            this.lblCommodity.Size = new System.Drawing.Size(69, 15);
            this.lblCommodity.TabIndex = 102;
            this.lblCommodity.Text = "Commodity";
            // 
            // lblUsername
            // 
            this.lblUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(542, 7);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(65, 15);
            this.lblUsername.TabIndex = 101;
            this.lblUsername.Text = "Username";
            // 
            // tbContNo2
            // 
            this.tbContNo2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbContNo2.Enabled = false;
            this.tbContNo2.Location = new System.Drawing.Point(543, 329);
            this.tbContNo2.Name = "tbContNo2";
            this.tbContNo2.Size = new System.Drawing.Size(242, 20);
            this.tbContNo2.TabIndex = 10;
            // 
            // lblContNo2
            // 
            this.lblContNo2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblContNo2.AutoSize = true;
            this.lblContNo2.Location = new System.Drawing.Point(423, 331);
            this.lblContNo2.Name = "lblContNo2";
            this.lblContNo2.Size = new System.Drawing.Size(92, 15);
            this.lblContNo2.TabIndex = 99;
            this.lblContNo2.Text = "Container 2 No.";
            // 
            // tbContNo1
            // 
            this.tbContNo1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbContNo1.Enabled = false;
            this.tbContNo1.Location = new System.Drawing.Point(543, 301);
            this.tbContNo1.Name = "tbContNo1";
            this.tbContNo1.Size = new System.Drawing.Size(242, 20);
            this.tbContNo1.TabIndex = 9;
            // 
            // lblContNo1
            // 
            this.lblContNo1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblContNo1.AutoSize = true;
            this.lblContNo1.Location = new System.Drawing.Point(423, 305);
            this.lblContNo1.Name = "lblContNo1";
            this.lblContNo1.Size = new System.Drawing.Size(92, 15);
            this.lblContNo1.TabIndex = 97;
            this.lblContNo1.Text = "Container 1 No.";
            // 
            // tbNote
            // 
            this.tbNote.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNote.Enabled = false;
            this.tbNote.Location = new System.Drawing.Point(544, 173);
            this.tbNote.Name = "tbNote";
            this.tbNote.Size = new System.Drawing.Size(242, 20);
            this.tbNote.TabIndex = 4;
            // 
            // lblNote
            // 
            this.lblNote.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNote.AutoSize = true;
            this.lblNote.Location = new System.Drawing.Point(423, 177);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(33, 15);
            this.lblNote.TabIndex = 94;
            this.lblNote.Text = "Note";
            // 
            // tbDONo
            // 
            this.tbDONo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDONo.Enabled = false;
            this.tbDONo.Location = new System.Drawing.Point(543, 275);
            this.tbDONo.Name = "tbDONo";
            this.tbDONo.Size = new System.Drawing.Size(242, 20);
            this.tbDONo.TabIndex = 8;
            // 
            // lblDONo
            // 
            this.lblDONo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDONo.AutoSize = true;
            this.lblDONo.Location = new System.Drawing.Point(423, 277);
            this.lblDONo.Name = "lblDONo";
            this.lblDONo.Size = new System.Drawing.Size(47, 15);
            this.lblDONo.TabIndex = 91;
            this.lblDONo.Text = "DO No.";
            // 
            // tbSealNo
            // 
            this.tbSealNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSealNo.Enabled = false;
            this.tbSealNo.Location = new System.Drawing.Point(543, 249);
            this.tbSealNo.Name = "tbSealNo";
            this.tbSealNo.Size = new System.Drawing.Size(242, 20);
            this.tbSealNo.TabIndex = 7;
            // 
            // lblSealNo
            // 
            this.lblSealNo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSealNo.AutoSize = true;
            this.lblSealNo.Location = new System.Drawing.Point(423, 251);
            this.lblSealNo.Name = "lblSealNo";
            this.lblSealNo.Size = new System.Drawing.Size(54, 15);
            this.lblSealNo.TabIndex = 89;
            this.lblSealNo.Text = "Seal No.";
            // 
            // tbJnsTruck
            // 
            this.tbJnsTruck.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbJnsTruck.Enabled = false;
            this.tbJnsTruck.Location = new System.Drawing.Point(544, 147);
            this.tbJnsTruck.Name = "tbJnsTruck";
            this.tbJnsTruck.Size = new System.Drawing.Size(242, 20);
            this.tbJnsTruck.TabIndex = 3;
            // 
            // lbJnsTruck
            // 
            this.lbJnsTruck.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbJnsTruck.AutoSize = true;
            this.lbJnsTruck.Location = new System.Drawing.Point(424, 148);
            this.lbJnsTruck.Name = "lbJnsTruck";
            this.lbJnsTruck.Size = new System.Drawing.Size(69, 15);
            this.lbJnsTruck.TabIndex = 84;
            this.lbJnsTruck.Text = "Jenis Truck";
            // 
            // lbDateTime
            // 
            this.lbDateTime.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDateTime.AutoSize = true;
            this.lbDateTime.Location = new System.Drawing.Point(542, 20);
            this.lbDateTime.Name = "lbDateTime";
            this.lbDateTime.Size = new System.Drawing.Size(64, 15);
            this.lbDateTime.TabIndex = 79;
            this.lbDateTime.Text = "Date Time";
            // 
            // gbWINKen
            // 
            this.gbWINKen.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbWINKen.Controls.Add(this.tbBaseSAP);
            this.gbWINKen.Controls.Add(this.tbMOISTKebun);
            this.gbWINKen.Controls.Add(this.tbFFAKebun);
            this.gbWINKen.Controls.Add(this.lbMOISTKebun);
            this.gbWINKen.Controls.Add(this.lbFFAKebun);
            this.gbWINKen.Controls.Add(this.tbNettoKebun);
            this.gbWINKen.Controls.Add(this.lblNettoKebun);
            this.gbWINKen.Controls.Add(this.tbTaraKebun);
            this.gbWINKen.Controls.Add(this.lblTaraKebun);
            this.gbWINKen.Controls.Add(this.tbBrutoKebun);
            this.gbWINKen.Controls.Add(this.lblBrutoKebun);
            this.gbWINKen.Controls.Add(this.gbUjiLab);
            this.gbWINKen.Controls.Add(this.cbTMManual);
            this.gbWINKen.Controls.Add(this.btnTimbangMasuk);
            this.gbWINKen.Controls.Add(this.tbQtyMasuk);
            this.gbWINKen.Location = new System.Drawing.Point(18, 405);
            this.gbWINKen.Name = "gbWINKen";
            this.gbWINKen.Size = new System.Drawing.Size(769, 184);
            this.gbWINKen.TabIndex = 20;
            this.gbWINKen.TabStop = false;
            this.gbWINKen.Text = "Timbangan Masuk";
            // 
            // tbBaseSAP
            // 
            this.tbBaseSAP.Location = new System.Drawing.Point(467, 190);
            this.tbBaseSAP.Name = "tbBaseSAP";
            this.tbBaseSAP.Size = new System.Drawing.Size(100, 20);
            this.tbBaseSAP.TabIndex = 27;
            // 
            // tbMOISTKebun
            // 
            this.tbMOISTKebun.Enabled = false;
            this.tbMOISTKebun.Location = new System.Drawing.Point(93, 126);
            this.tbMOISTKebun.Name = "tbMOISTKebun";
            this.tbMOISTKebun.Size = new System.Drawing.Size(129, 20);
            this.tbMOISTKebun.TabIndex = 5;
            // 
            // tbFFAKebun
            // 
            this.tbFFAKebun.Enabled = false;
            this.tbFFAKebun.Location = new System.Drawing.Point(93, 100);
            this.tbFFAKebun.Name = "tbFFAKebun";
            this.tbFFAKebun.Size = new System.Drawing.Size(129, 20);
            this.tbFFAKebun.TabIndex = 4;
            // 
            // lbMOISTKebun
            // 
            this.lbMOISTKebun.AutoSize = true;
            this.lbMOISTKebun.Location = new System.Drawing.Point(10, 129);
            this.lbMOISTKebun.Name = "lbMOISTKebun";
            this.lbMOISTKebun.Size = new System.Drawing.Size(84, 15);
            this.lbMOISTKebun.TabIndex = 26;
            this.lbMOISTKebun.Text = "MOIST Kebun";
            // 
            // lbFFAKebun
            // 
            this.lbFFAKebun.AutoSize = true;
            this.lbFFAKebun.Location = new System.Drawing.Point(10, 103);
            this.lbFFAKebun.Name = "lbFFAKebun";
            this.lbFFAKebun.Size = new System.Drawing.Size(67, 15);
            this.lbFFAKebun.TabIndex = 24;
            this.lbFFAKebun.Text = "FFA Kebun";
            // 
            // tbNettoKebun
            // 
            this.tbNettoKebun.Enabled = false;
            this.tbNettoKebun.Location = new System.Drawing.Point(93, 73);
            this.tbNettoKebun.Name = "tbNettoKebun";
            this.tbNettoKebun.Size = new System.Drawing.Size(129, 20);
            this.tbNettoKebun.TabIndex = 3;
            // 
            // lblNettoKebun
            // 
            this.lblNettoKebun.AutoSize = true;
            this.lblNettoKebun.Location = new System.Drawing.Point(10, 76);
            this.lblNettoKebun.Name = "lblNettoKebun";
            this.lblNettoKebun.Size = new System.Drawing.Size(78, 15);
            this.lblNettoKebun.TabIndex = 22;
            this.lblNettoKebun.Text = "Nettto Kebun";
            // 
            // tbTaraKebun
            // 
            this.tbTaraKebun.Enabled = false;
            this.tbTaraKebun.Location = new System.Drawing.Point(93, 47);
            this.tbTaraKebun.Name = "tbTaraKebun";
            this.tbTaraKebun.Size = new System.Drawing.Size(129, 20);
            this.tbTaraKebun.TabIndex = 2;
            // 
            // lblTaraKebun
            // 
            this.lblTaraKebun.AutoSize = true;
            this.lblTaraKebun.Location = new System.Drawing.Point(10, 50);
            this.lblTaraKebun.Name = "lblTaraKebun";
            this.lblTaraKebun.Size = new System.Drawing.Size(71, 15);
            this.lblTaraKebun.TabIndex = 20;
            this.lblTaraKebun.Text = "Tara Kebun";
            // 
            // tbBrutoKebun
            // 
            this.tbBrutoKebun.Enabled = false;
            this.tbBrutoKebun.Location = new System.Drawing.Point(93, 21);
            this.tbBrutoKebun.Name = "tbBrutoKebun";
            this.tbBrutoKebun.Size = new System.Drawing.Size(129, 20);
            this.tbBrutoKebun.TabIndex = 1;
            // 
            // lblBrutoKebun
            // 
            this.lblBrutoKebun.AutoSize = true;
            this.lblBrutoKebun.Location = new System.Drawing.Point(10, 24);
            this.lblBrutoKebun.Name = "lblBrutoKebun";
            this.lblBrutoKebun.Size = new System.Drawing.Size(75, 15);
            this.lblBrutoKebun.TabIndex = 18;
            this.lblBrutoKebun.Text = "Bruto Kebun";
            // 
            // gbUjiLab
            // 
            this.gbUjiLab.Controls.Add(this.tbDOBI);
            this.gbUjiLab.Controls.Add(this.tbIMP);
            this.gbUjiLab.Controls.Add(this.tbMOIST);
            this.gbUjiLab.Controls.Add(this.tbCAROTINE);
            this.gbUjiLab.Controls.Add(this.lblCAROTINE);
            this.gbUjiLab.Controls.Add(this.lblDOBI);
            this.gbUjiLab.Controls.Add(this.lblIMP);
            this.gbUjiLab.Controls.Add(this.lblMoist);
            this.gbUjiLab.Controls.Add(this.tbFFA);
            this.gbUjiLab.Controls.Add(this.lblFFA);
            this.gbUjiLab.Location = new System.Drawing.Point(243, 20);
            this.gbUjiLab.Margin = new System.Windows.Forms.Padding(2);
            this.gbUjiLab.Name = "gbUjiLab";
            this.gbUjiLab.Padding = new System.Windows.Forms.Padding(2);
            this.gbUjiLab.Size = new System.Drawing.Size(245, 155);
            this.gbUjiLab.TabIndex = 5;
            this.gbUjiLab.TabStop = false;
            this.gbUjiLab.Text = "Uji Lab";
            // 
            // tbDOBI
            // 
            this.tbDOBI.Enabled = false;
            this.tbDOBI.Location = new System.Drawing.Point(79, 97);
            this.tbDOBI.Margin = new System.Windows.Forms.Padding(2);
            this.tbDOBI.Name = "tbDOBI";
            this.tbDOBI.Size = new System.Drawing.Size(156, 20);
            this.tbDOBI.TabIndex = 3;
            // 
            // tbIMP
            // 
            this.tbIMP.Enabled = false;
            this.tbIMP.Location = new System.Drawing.Point(79, 73);
            this.tbIMP.Margin = new System.Windows.Forms.Padding(2);
            this.tbIMP.Name = "tbIMP";
            this.tbIMP.Size = new System.Drawing.Size(156, 20);
            this.tbIMP.TabIndex = 2;
            // 
            // tbMOIST
            // 
            this.tbMOIST.Enabled = false;
            this.tbMOIST.Location = new System.Drawing.Point(79, 48);
            this.tbMOIST.Margin = new System.Windows.Forms.Padding(2);
            this.tbMOIST.Name = "tbMOIST";
            this.tbMOIST.Size = new System.Drawing.Size(156, 20);
            this.tbMOIST.TabIndex = 1;
            // 
            // tbCAROTINE
            // 
            this.tbCAROTINE.Enabled = false;
            this.tbCAROTINE.Location = new System.Drawing.Point(79, 125);
            this.tbCAROTINE.Margin = new System.Windows.Forms.Padding(2);
            this.tbCAROTINE.Name = "tbCAROTINE";
            this.tbCAROTINE.Size = new System.Drawing.Size(156, 20);
            this.tbCAROTINE.TabIndex = 4;
            // 
            // lblCAROTINE
            // 
            this.lblCAROTINE.AutoSize = true;
            this.lblCAROTINE.Location = new System.Drawing.Point(6, 127);
            this.lblCAROTINE.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblCAROTINE.Name = "lblCAROTINE";
            this.lblCAROTINE.Size = new System.Drawing.Size(67, 15);
            this.lblCAROTINE.TabIndex = 5;
            this.lblCAROTINE.Text = "CAROTINE";
            // 
            // lblDOBI
            // 
            this.lblDOBI.AutoSize = true;
            this.lblDOBI.Location = new System.Drawing.Point(6, 101);
            this.lblDOBI.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblDOBI.Name = "lblDOBI";
            this.lblDOBI.Size = new System.Drawing.Size(36, 15);
            this.lblDOBI.TabIndex = 4;
            this.lblDOBI.Text = "DOBI";
            // 
            // lblIMP
            // 
            this.lblIMP.AutoSize = true;
            this.lblIMP.Location = new System.Drawing.Point(6, 75);
            this.lblIMP.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblIMP.Name = "lblIMP";
            this.lblIMP.Size = new System.Drawing.Size(29, 15);
            this.lblIMP.TabIndex = 3;
            this.lblIMP.Text = "IMP";
            // 
            // lblMoist
            // 
            this.lblMoist.AutoSize = true;
            this.lblMoist.Location = new System.Drawing.Point(4, 48);
            this.lblMoist.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblMoist.Name = "lblMoist";
            this.lblMoist.Size = new System.Drawing.Size(45, 15);
            this.lblMoist.TabIndex = 2;
            this.lblMoist.Text = "MOIST";
            // 
            // tbFFA
            // 
            this.tbFFA.Enabled = false;
            this.tbFFA.Location = new System.Drawing.Point(79, 21);
            this.tbFFA.Margin = new System.Windows.Forms.Padding(2);
            this.tbFFA.Name = "tbFFA";
            this.tbFFA.Size = new System.Drawing.Size(156, 20);
            this.tbFFA.TabIndex = 0;
            // 
            // lblFFA
            // 
            this.lblFFA.AutoSize = true;
            this.lblFFA.Location = new System.Drawing.Point(4, 23);
            this.lblFFA.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lblFFA.Name = "lblFFA";
            this.lblFFA.Size = new System.Drawing.Size(28, 15);
            this.lblFFA.TabIndex = 0;
            this.lblFFA.Text = "FFA";
            // 
            // cbTMManual
            // 
            this.cbTMManual.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cbTMManual.AutoSize = true;
            this.cbTMManual.Enabled = false;
            this.cbTMManual.Location = new System.Drawing.Point(556, 56);
            this.cbTMManual.Name = "cbTMManual";
            this.cbTMManual.Size = new System.Drawing.Size(71, 19);
            this.cbTMManual.TabIndex = 6;
            this.cbTMManual.Text = "Manual";
            this.cbTMManual.UseVisualStyleBackColor = true;
            this.cbTMManual.CheckedChanged += new System.EventHandler(this.cbTMManual_CheckedChanged_1);
            // 
            // btnTimbangMasuk
            // 
            this.btnTimbangMasuk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnTimbangMasuk.Enabled = false;
            this.btnTimbangMasuk.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTimbangMasuk.Location = new System.Drawing.Point(631, 48);
            this.btnTimbangMasuk.Name = "btnTimbangMasuk";
            this.btnTimbangMasuk.Size = new System.Drawing.Size(132, 28);
            this.btnTimbangMasuk.TabIndex = 7;
            this.btnTimbangMasuk.Text = "Timbang Masuk";
            this.btnTimbangMasuk.UseVisualStyleBackColor = true;
            this.btnTimbangMasuk.Click += new System.EventHandler(this.btnTimbangMasuk_Click);
            // 
            // tbQtyMasuk
            // 
            this.tbQtyMasuk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbQtyMasuk.BackColor = System.Drawing.Color.CadetBlue;
            this.tbQtyMasuk.Enabled = false;
            this.tbQtyMasuk.Font = new System.Drawing.Font("Segoe UI", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbQtyMasuk.Location = new System.Drawing.Point(553, 85);
            this.tbQtyMasuk.Name = "tbQtyMasuk";
            this.tbQtyMasuk.Size = new System.Drawing.Size(212, 61);
            this.tbQtyMasuk.TabIndex = 0;
            this.tbQtyMasuk.Text = "0";
            this.tbQtyMasuk.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tbQtyMasuk.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbQtyMasuk_KeyPress);
            // 
            // btnWBSave
            // 
            this.btnWBSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnWBSave.Enabled = false;
            this.btnWBSave.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnWBSave.Location = new System.Drawing.Point(691, 595);
            this.btnWBSave.Name = "btnWBSave";
            this.btnWBSave.Size = new System.Drawing.Size(96, 25);
            this.btnWBSave.TabIndex = 25;
            this.btnWBSave.Text = "Save";
            this.btnWBSave.UseVisualStyleBackColor = true;
            this.btnWBSave.Click += new System.EventHandler(this.btnWBSave_Click);
            // 
            // tbNopol
            // 
            this.tbNopol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNopol.Enabled = false;
            this.tbNopol.Location = new System.Drawing.Point(544, 68);
            this.tbNopol.Name = "tbNopol";
            this.tbNopol.Size = new System.Drawing.Size(242, 20);
            this.tbNopol.TabIndex = 0;
            // 
            // tbSIMSupir
            // 
            this.tbSIMSupir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSIMSupir.Enabled = false;
            this.tbSIMSupir.Location = new System.Drawing.Point(544, 121);
            this.tbSIMSupir.Name = "tbSIMSupir";
            this.tbSIMSupir.Size = new System.Drawing.Size(242, 20);
            this.tbSIMSupir.TabIndex = 2;
            // 
            // lbWINNopol
            // 
            this.lbWINNopol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbWINNopol.AutoSize = true;
            this.lbWINNopol.Location = new System.Drawing.Point(424, 69);
            this.lbWINNopol.Name = "lbWINNopol";
            this.lbWINNopol.Size = new System.Drawing.Size(78, 15);
            this.lbWINNopol.TabIndex = 69;
            this.lbWINNopol.Text = "Nomor Polisi";
            // 
            // lbWINSupir
            // 
            this.lbWINSupir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbWINSupir.AutoSize = true;
            this.lbWINSupir.Location = new System.Drawing.Point(424, 96);
            this.lbWINSupir.Name = "lbWINSupir";
            this.lbWINSupir.Size = new System.Drawing.Size(73, 15);
            this.lbWINSupir.TabIndex = 71;
            this.lbWINSupir.Text = "Nama Supir";
            // 
            // tbNamaSupir
            // 
            this.tbNamaSupir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbNamaSupir.Enabled = false;
            this.tbNamaSupir.Location = new System.Drawing.Point(544, 94);
            this.tbNamaSupir.Name = "tbNamaSupir";
            this.tbNamaSupir.Size = new System.Drawing.Size(242, 20);
            this.tbNamaSupir.TabIndex = 1;
            // 
            // lbWINEksCod
            // 
            this.lbWINEksCod.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbWINEksCod.AutoSize = true;
            this.lbWINEksCod.Location = new System.Drawing.Point(424, 122);
            this.lbWINEksCod.Name = "lbWINEksCod";
            this.lbWINEksCod.Size = new System.Drawing.Size(80, 15);
            this.lbWINEksCod.TabIndex = 72;
            this.lbWINEksCod.Text = "No SIM Supir";
            // 
            // runningTime
            // 
            this.runningTime.Tick += new System.EventHandler(this.runningTime_Tick);
            // 
            // tbTransporterEks
            // 
            this.tbTransporterEks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTransporterEks.Enabled = false;
            this.tbTransporterEks.Location = new System.Drawing.Point(544, 225);
            this.tbTransporterEks.Name = "tbTransporterEks";
            this.tbTransporterEks.Size = new System.Drawing.Size(242, 20);
            this.tbTransporterEks.TabIndex = 6;
            // 
            // lblTransporterEks
            // 
            this.lblTransporterEks.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransporterEks.AutoSize = true;
            this.lblTransporterEks.Location = new System.Drawing.Point(424, 227);
            this.lblTransporterEks.Name = "lblTransporterEks";
            this.lblTransporterEks.Size = new System.Drawing.Size(124, 15);
            this.lblTransporterEks.TabIndex = 106;
            this.lblTransporterEks.Text = "Transporter Eksternal";
            // 
            // lblTransporterInt
            // 
            this.lblTransporterInt.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTransporterInt.AutoSize = true;
            this.lblTransporterInt.Location = new System.Drawing.Point(424, 201);
            this.lblTransporterInt.Name = "lblTransporterInt";
            this.lblTransporterInt.Size = new System.Drawing.Size(114, 15);
            this.lblTransporterInt.TabIndex = 104;
            this.lblTransporterInt.Text = "Transporter Internal";
            // 
            // gbPabean
            // 
            this.gbPabean.Controls.Add(this.tbNomorSPB);
            this.gbPabean.Controls.Add(this.lblNomorSPB);
            this.gbPabean.Controls.Add(this.dtTglSPB);
            this.gbPabean.Controls.Add(this.lbTglSPB);
            this.gbPabean.Controls.Add(this.cbStatusJalur);
            this.gbPabean.Controls.Add(this.lbStatusJalur);
            this.gbPabean.Controls.Add(this.cbFasilitasPabean);
            this.gbPabean.Controls.Add(this.lbFasilitasPabean);
            this.gbPabean.Controls.Add(this.cmbKodeDokPabean);
            this.gbPabean.Controls.Add(this.lbKodeDokPabean);
            this.gbPabean.Controls.Add(this.tbNoDokPabean);
            this.gbPabean.Controls.Add(this.lbNoDokPabean);
            this.gbPabean.Controls.Add(this.dtTglDokPabean);
            this.gbPabean.Controls.Add(this.lbTglDokPabean);
            this.gbPabean.Controls.Add(this.tbNomorAju);
            this.gbPabean.Controls.Add(this.lblNomorAju);
            this.gbPabean.Location = new System.Drawing.Point(20, 106);
            this.gbPabean.Name = "gbPabean";
            this.gbPabean.Size = new System.Drawing.Size(380, 205);
            this.gbPabean.TabIndex = 18;
            this.gbPabean.TabStop = false;
            this.gbPabean.Text = "Informasi Pabean";
            // 
            // lblNomorSPB
            // 
            this.lblNomorSPB.AutoSize = true;
            this.lblNomorSPB.Location = new System.Drawing.Point(8, 20);
            this.lblNomorSPB.Name = "lblNomorSPB";
            this.lblNomorSPB.Size = new System.Drawing.Size(72, 15);
            this.lblNomorSPB.TabIndex = 31;
            this.lblNomorSPB.Text = "Nomor SPB";
            // 
            // tbNomorSPB
            // 
            this.tbNomorSPB.Enabled = false;
            this.tbNomorSPB.Location = new System.Drawing.Point(10, 36);
            this.tbNomorSPB.Name = "tbNomorSPB";
            this.tbNomorSPB.Size = new System.Drawing.Size(170, 20);
            this.tbNomorSPB.TabIndex = 0;
            // 
            // lbTglSPB
            // 
            this.lbTglSPB.AutoSize = true;
            this.lbTglSPB.Location = new System.Drawing.Point(8, 64);
            this.lbTglSPB.Name = "lbTglSPB";
            this.lbTglSPB.Size = new System.Drawing.Size(77, 15);
            this.lbTglSPB.TabIndex = 32;
            this.lbTglSPB.Text = "Tanggal SPB";
            // 
            // dtTglSPB
            // 
            this.dtTglSPB.Enabled = false;
            this.dtTglSPB.Location = new System.Drawing.Point(10, 80);
            this.dtTglSPB.Name = "dtTglSPB";
            this.dtTglSPB.Size = new System.Drawing.Size(170, 20);
            this.dtTglSPB.TabIndex = 1;
            // 
            // lbStatusJalur
            // 
            this.lbStatusJalur.AutoSize = true;
            this.lbStatusJalur.Location = new System.Drawing.Point(8, 108);
            this.lbStatusJalur.Name = "lbStatusJalur";
            this.lbStatusJalur.Size = new System.Drawing.Size(71, 15);
            this.lbStatusJalur.TabIndex = 34;
            this.lbStatusJalur.Text = "Status Jalur";
            // 
            // cbStatusJalur
            // 
            this.cbStatusJalur.Enabled = false;
            this.cbStatusJalur.FormattingEnabled = true;
            this.cbStatusJalur.Location = new System.Drawing.Point(10, 124);
            this.cbStatusJalur.Name = "cbStatusJalur";
            this.cbStatusJalur.Size = new System.Drawing.Size(170, 21);
            this.cbStatusJalur.TabIndex = 2;
            // 
            // lbFasilitasPabean
            // 
            this.lbFasilitasPabean.AutoSize = true;
            this.lbFasilitasPabean.Location = new System.Drawing.Point(8, 152);
            this.lbFasilitasPabean.Name = "lbFasilitasPabean";
            this.lbFasilitasPabean.Size = new System.Drawing.Size(95, 15);
            this.lbFasilitasPabean.TabIndex = 36;
            this.lbFasilitasPabean.Text = "Fasilitas Pabean";
            // 
            // cbFasilitasPabean
            // 
            this.cbFasilitasPabean.Enabled = false;
            this.cbFasilitasPabean.FormattingEnabled = true;
            this.cbFasilitasPabean.Location = new System.Drawing.Point(10, 168);
            this.cbFasilitasPabean.Name = "cbFasilitasPabean";
            this.cbFasilitasPabean.Size = new System.Drawing.Size(170, 21);
            this.cbFasilitasPabean.TabIndex = 3;
            // 
            // lbKodeDokPabean
            // 
            this.lbKodeDokPabean.AutoSize = true;
            this.lbKodeDokPabean.Location = new System.Drawing.Point(193, 20);
            this.lbKodeDokPabean.Name = "lbKodeDokPabean";
            this.lbKodeDokPabean.Size = new System.Drawing.Size(139, 15);
            this.lbKodeDokPabean.TabIndex = 24;
            this.lbKodeDokPabean.Text = "Kode Dokumen Pabean";
            // 
            // cmbKodeDokPabean
            // 
            this.cmbKodeDokPabean.Enabled = false;
            this.cmbKodeDokPabean.FormattingEnabled = true;
            this.cmbKodeDokPabean.Location = new System.Drawing.Point(195, 36);
            this.cmbKodeDokPabean.Name = "cmbKodeDokPabean";
            this.cmbKodeDokPabean.Size = new System.Drawing.Size(170, 21);
            this.cmbKodeDokPabean.TabIndex = 4;
            // 
            // lbNoDokPabean
            // 
            this.lbNoDokPabean.AutoSize = true;
            this.lbNoDokPabean.Location = new System.Drawing.Point(193, 64);
            this.lbNoDokPabean.Name = "lbNoDokPabean";
            this.lbNoDokPabean.Size = new System.Drawing.Size(148, 15);
            this.lbNoDokPabean.TabIndex = 26;
            this.lbNoDokPabean.Text = "Nomor Dokumen Pabean";
            // 
            // tbNoDokPabean
            // 
            this.tbNoDokPabean.Enabled = false;
            this.tbNoDokPabean.Location = new System.Drawing.Point(195, 80);
            this.tbNoDokPabean.Name = "tbNoDokPabean";
            this.tbNoDokPabean.Size = new System.Drawing.Size(170, 20);
            this.tbNoDokPabean.TabIndex = 5;
            // 
            // lbTglDokPabean
            // 
            this.lbTglDokPabean.AutoSize = true;
            this.lbTglDokPabean.Location = new System.Drawing.Point(193, 108);
            this.lbTglDokPabean.Name = "lbTglDokPabean";
            this.lbTglDokPabean.Size = new System.Drawing.Size(155, 15);
            this.lbTglDokPabean.TabIndex = 28;
            this.lbTglDokPabean.Text = "Tanggal Dokumen Pabean";
            // 
            // dtTglDokPabean
            // 
            this.dtTglDokPabean.Enabled = false;
            this.dtTglDokPabean.Location = new System.Drawing.Point(195, 124);
            this.dtTglDokPabean.Name = "dtTglDokPabean";
            this.dtTglDokPabean.Size = new System.Drawing.Size(170, 20);
            this.dtTglDokPabean.TabIndex = 6;
            // 
            // lblNomorAju
            // 
            this.lblNomorAju.AutoSize = true;
            this.lblNomorAju.Location = new System.Drawing.Point(193, 152);
            this.lblNomorAju.Name = "lblNomorAju";
            this.lblNomorAju.Size = new System.Drawing.Size(65, 15);
            this.lblNomorAju.TabIndex = 33;
            this.lblNomorAju.Text = "Nomor Aju";
            // 
            // tbNomorAju
            // 
            this.tbNomorAju.Enabled = false;
            this.tbNomorAju.Location = new System.Drawing.Point(195, 168);
            this.tbNomorAju.Name = "tbNomorAju";
            this.tbNomorAju.Size = new System.Drawing.Size(170, 20);
            this.tbNomorAju.TabIndex = 7;
            // 
            // lblValLineNumSAP
            // 
            this.lblValLineNumSAP.AutoSize = true;
            this.lblValLineNumSAP.Location = new System.Drawing.Point(330, 246);
            this.lblValLineNumSAP.Name = "lblValLineNumSAP";
            this.lblValLineNumSAP.Size = new System.Drawing.Size(81, 15);
            this.lblValLineNumSAP.TabIndex = 88;
            this.lblValLineNumSAP.Text = "LineNumSAP";
            this.lblValLineNumSAP.Visible = false;
            // 
            // btnRefreshData
            // 
            this.btnRefreshData.Location = new System.Drawing.Point(327, 11);
            this.btnRefreshData.Name = "btnRefreshData";
            this.btnRefreshData.Size = new System.Drawing.Size(75, 23);
            this.btnRefreshData.TabIndex = 87;
            this.btnRefreshData.Text = "Refresh";
            this.btnRefreshData.UseVisualStyleBackColor = true;
            this.btnRefreshData.Visible = false;
            this.btnRefreshData.Click += new System.EventHandler(this.btnRefreshData_Click);
            // 
            // lbValDocEntSAP
            // 
            this.lbValDocEntSAP.AutoSize = true;
            this.lbValDocEntSAP.Location = new System.Drawing.Point(330, 223);
            this.lbValDocEntSAP.Name = "lbValDocEntSAP";
            this.lbValDocEntSAP.Size = new System.Drawing.Size(79, 15);
            this.lbValDocEntSAP.TabIndex = 86;
            this.lbValDocEntSAP.Text = "DocEntrySAP";
            this.lbValDocEntSAP.Visible = false;
            // 
            // cbDocType
            // 
            this.cbDocType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbDocType.FormattingEnabled = true;
            this.cbDocType.Items.AddRange(new object[] {
            "PO",
            "SO"});
            this.cbDocType.Location = new System.Drawing.Point(20, 28);
            this.cbDocType.Name = "cbDocType";
            this.cbDocType.Size = new System.Drawing.Size(49, 25);
            this.cbDocType.TabIndex = 81;
            // 
            // btnCariBaseDoc
            // 
            this.btnCariBaseDoc.Location = new System.Drawing.Point(302, 50);
            this.btnCariBaseDoc.Name = "btnCariBaseDoc";
            this.btnCariBaseDoc.Size = new System.Drawing.Size(56, 20);
            this.btnCariBaseDoc.TabIndex = 78;
            this.btnCariBaseDoc.Text = "Search";
            this.btnCariBaseDoc.UseVisualStyleBackColor = true;
            this.btnCariBaseDoc.Click += new System.EventHandler(this.btnCariBaseDoc_Click);
            // 
            // lbTiketNum
            // 
            this.lbTiketNum.AutoSize = true;
            this.lbTiketNum.Location = new System.Drawing.Point(18, 56);
            this.lbTiketNum.Name = "lbTiketNum";
            this.lbTiketNum.Size = new System.Drawing.Size(74, 15);
            this.lbTiketNum.TabIndex = 70;
            this.lbTiketNum.Text = "Nomor Tiket";
            // 
            // tbTiketNum
            // 
            this.tbTiketNum.Enabled = false;
            this.tbTiketNum.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbTiketNum.Location = new System.Drawing.Point(20, 72);
            this.tbTiketNum.Name = "tbTiketNum";
            this.tbTiketNum.Size = new System.Drawing.Size(268, 24);
            this.tbTiketNum.TabIndex = 73;
            // 
            // lbWINBaseDoc
            // 
            this.lbWINBaseDoc.AutoSize = true;
            this.lbWINBaseDoc.Location = new System.Drawing.Point(18, 11);
            this.lbWINBaseDoc.Name = "lbWINBaseDoc";
            this.lbWINBaseDoc.Size = new System.Drawing.Size(95, 15);
            this.lbWINBaseDoc.TabIndex = 75;
            this.lbWINBaseDoc.Text = "Base Document";
            // 
            // cbTransInt
            // 
            this.cbTransInt.Location = new System.Drawing.Point(544, 198);
            this.cbTransInt.Margin = new System.Windows.Forms.Padding(2);
            this.cbTransInt.Name = "cbTransInt";
            this.cbTransInt.Size = new System.Drawing.Size(242, 21);
            this.cbTransInt.TabIndex = 0;
            this.cbTransInt.SelectedIndexChanged += new System.EventHandler(this.cbTransInt_SelectedIndexChanged);
            // 
            // tbBaseDoc
            // 
            this.tbBaseDoc.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbBaseDoc.Location = new System.Drawing.Point(74, 28);
            this.tbBaseDoc.Name = "tbBaseDoc";
            this.tbBaseDoc.Size = new System.Drawing.Size(215, 24);
            this.tbBaseDoc.TabIndex = 77;
            this.tbBaseDoc.TextChanged += new System.EventHandler(this.tbBaseDoc_TextChanged);
            // 
            // WeighbridgeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(798, 628);
            this.Controls.Add(this.cbTransInt);
            this.Controls.Add(this.lbWINBaseDoc);
            this.Controls.Add(this.tbTiketNum);
            this.Controls.Add(this.lbTiketNum);
            this.Controls.Add(this.tbTransporterEks);
            this.Controls.Add(this.tbBaseDoc);
            this.Controls.Add(this.lblTransporterEks);
            this.Controls.Add(this.btnCariBaseDoc);
            this.Controls.Add(this.cbDocType);
            this.Controls.Add(this.lblTransporterInt);
            this.Controls.Add(this.lbValDocEntSAP);
            this.Controls.Add(this.tbCommodity);
            this.Controls.Add(this.btnRefreshData);
            this.Controls.Add(this.lblCommodity);
            this.Controls.Add(this.lblValLineNumSAP);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.gbPabean);
            this.Controls.Add(this.tbContNo2);
            this.Controls.Add(this.lblContNo2);
            this.Controls.Add(this.tbContNo1);
            this.Controls.Add(this.lblContNo1);
            this.Controls.Add(this.tbNote);
            this.Controls.Add(this.lblNote);
            this.Controls.Add(this.tbDONo);
            this.Controls.Add(this.lblDONo);
            this.Controls.Add(this.tbSealNo);
            this.Controls.Add(this.lblSealNo);
            this.Controls.Add(this.tbJnsTruck);
            this.Controls.Add(this.lbJnsTruck);
            this.Controls.Add(this.lbDateTime);
            this.Controls.Add(this.gbWINKen);
            this.Controls.Add(this.btnWBSave);
            this.Controls.Add(this.tbNopol);
            this.Controls.Add(this.tbSIMSupir);
            this.Controls.Add(this.lbWINNopol);
            this.Controls.Add(this.lbWINSupir);
            this.Controls.Add(this.tbNamaSupir);
            this.Controls.Add(this.lbWINEksCod);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WeighbridgeForm";
            this.Text = "Timbang Masuk - PT Tanimas Resources International";
            this.Load += new System.EventHandler(this.WeighbridgeForm_Load);
            this.gbWINKen.ResumeLayout(false);
            this.gbWINKen.PerformLayout();
            this.gbUjiLab.ResumeLayout(false);
            this.gbUjiLab.PerformLayout();
            this.gbPabean.ResumeLayout(false);
            this.gbPabean.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbCommodity;
        private System.Windows.Forms.Label lblCommodity;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox tbContNo2;
        private System.Windows.Forms.Label lblContNo2;
        private System.Windows.Forms.TextBox tbContNo1;
        private System.Windows.Forms.Label lblContNo1;
        private System.Windows.Forms.TextBox tbNote;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.TextBox tbDONo;
        private System.Windows.Forms.Label lblDONo;
        private System.Windows.Forms.TextBox tbSealNo;
        private System.Windows.Forms.Label lblSealNo;
        private System.Windows.Forms.TextBox tbJnsTruck;
        private System.Windows.Forms.Label lbJnsTruck;
        private System.Windows.Forms.Label lbDateTime;
        private System.Windows.Forms.GroupBox gbWINKen;
        private System.Windows.Forms.TextBox tbMOISTKebun;
        private System.Windows.Forms.TextBox tbFFAKebun;
        private System.Windows.Forms.Label lbMOISTKebun;
        private System.Windows.Forms.Label lbFFAKebun;
        private System.Windows.Forms.TextBox tbNettoKebun;
        private System.Windows.Forms.Label lblNettoKebun;
        private System.Windows.Forms.TextBox tbTaraKebun;
        private System.Windows.Forms.Label lblTaraKebun;
        private System.Windows.Forms.TextBox tbBrutoKebun;
        private System.Windows.Forms.Label lblBrutoKebun;
        private System.Windows.Forms.GroupBox gbUjiLab;
        private System.Windows.Forms.TextBox tbDOBI;
        private System.Windows.Forms.TextBox tbIMP;
        private System.Windows.Forms.TextBox tbMOIST;
        private System.Windows.Forms.TextBox tbCAROTINE;
        private System.Windows.Forms.Label lblCAROTINE;
        private System.Windows.Forms.Label lblDOBI;
        private System.Windows.Forms.Label lblIMP;
        private System.Windows.Forms.Label lblMoist;
        private System.Windows.Forms.TextBox tbFFA;
        private System.Windows.Forms.Label lblFFA;
        private System.Windows.Forms.CheckBox cbTMManual;
        private System.Windows.Forms.Button btnTimbangMasuk;
        private System.Windows.Forms.TextBox tbQtyMasuk;
        private System.Windows.Forms.Button btnWBSave;
        private System.Windows.Forms.TextBox tbNopol;
        private System.Windows.Forms.TextBox tbSIMSupir;
        private System.Windows.Forms.Label lbWINNopol;
        private System.Windows.Forms.Label lbWINSupir;
        private System.Windows.Forms.TextBox tbNamaSupir;
        private System.Windows.Forms.Label lbWINEksCod;
        private System.Windows.Forms.Timer runningTime;
        private System.Windows.Forms.TextBox tbTransporterEks;
        private System.Windows.Forms.Label lblTransporterEks;
        private System.Windows.Forms.Label lblTransporterInt;
        private System.Windows.Forms.GroupBox gbPabean;
        private System.Windows.Forms.TextBox tbNomorAju;
        private System.Windows.Forms.Label lblNomorAju;
        private System.Windows.Forms.TextBox tbNomorSPB;
        private System.Windows.Forms.Label lblNomorSPB;
        private System.Windows.Forms.DateTimePicker dtTglSPB;
        private System.Windows.Forms.Label lbTglSPB;
        private System.Windows.Forms.ComboBox cbStatusJalur;
        private System.Windows.Forms.Label lbStatusJalur;
        private System.Windows.Forms.ComboBox cbFasilitasPabean;
        private System.Windows.Forms.Label lbFasilitasPabean;
        private System.Windows.Forms.ComboBox cmbKodeDokPabean;
        private System.Windows.Forms.DateTimePicker dtTglDokPabean;
        private System.Windows.Forms.Label lbTglDokPabean;
        private System.Windows.Forms.TextBox tbNoDokPabean;
        private System.Windows.Forms.Label lbNoDokPabean;
        private System.Windows.Forms.Label lbKodeDokPabean;
        private System.Windows.Forms.Label lblValLineNumSAP;
        private System.Windows.Forms.Button btnRefreshData;
        private System.Windows.Forms.Label lbValDocEntSAP;
        private System.Windows.Forms.ComboBox cbDocType;
        private System.Windows.Forms.Button btnCariBaseDoc;
        private System.Windows.Forms.Label lbTiketNum;
        private System.Windows.Forms.TextBox tbTiketNum;
        private System.Windows.Forms.Label lbWINBaseDoc;
        private System.Windows.Forms.ComboBox cbTransInt;
        private System.Windows.Forms.TextBox tbBaseSAP;
        private System.Windows.Forms.TextBox tbBaseDoc;
    }
}