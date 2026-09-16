using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Windows.Forms;
using Sap.Data.Hana;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;

namespace Weighbridge
{
    public partial class WeighbridgeForm : Form
    {
        private SerialPort _serialPort;
        private WeighStatusForm _statusForm;

        HANAConnetion hanaConn = new HANAConnetion();
        SBOConnection oSBOConnection = new SBOConnection();

        public string timbangType; //A = Auto, M = Manual

        private string noKontrak = "";
        private double sisaKuotaPO = 0;
        private double totalKuotaPO = 0;
        private string statusDocPO = "";

        public WeighbridgeForm()
        {
            InitializeComponent();
        }

        private void WeighbridgeForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataKodeOokumenPabean();
            LoadDataComboBox();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;

            dtTglDokPabean.Format = DateTimePickerFormat.Custom;
            dtTglDokPabean.CustomFormat = " ";
            dtTglDokPabean.ShowCheckBox = true;
            dtTglDokPabean.Checked = false;

            dtTglSPB.Format = DateTimePickerFormat.Custom;
            dtTglSPB.CustomFormat = " ";
            dtTglSPB.ShowCheckBox = true;
            dtTglSPB.Checked = false;

            cbStatusJalur.Items.Clear();
            cbStatusJalur.Items.AddRange(new object[] { "Hijau", "Kuning", "Merah", "MITA" });

            cbFasilitasPabean.Items.Clear();
            cbFasilitasPabean.Items.AddRange(new object[] { "Fasilitas", "Non Fasilitas" });

            dtTglDokPabean.ValueChanged += (s, ev) =>
            {
                if (dtTglDokPabean.Checked) dtTglDokPabean.CustomFormat = "yyyy-MM-dd";
                else dtTglDokPabean.CustomFormat = " ";
            };

            dtTglSPB.ValueChanged += (s, ev) =>
            {
                if (dtTglSPB.Checked) dtTglSPB.CustomFormat = "yyyy-MM-dd";
                else dtTglSPB.CustomFormat = " ";
            };

            cbTransInt.Enabled = false;
        }

        private void InitializeSerialPort()
        {
            _serialPort = new SerialPort()
            {
                PortName = ConfigManager.wbConfig.portName,        // Change to your COM port
                BaudRate = Convert.ToInt32(ConfigManager.wbConfig.baudRate),          // Common: 9600, 19200, 38400
                DataBits = Convert.ToInt32(ConfigManager.wbConfig.dataBits),
                Parity = Parity.Even,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = Convert.ToInt32(ConfigManager.wbConfig.readTimeout),       // Increase timeout to 3 seconds
                WriteTimeout = Convert.ToInt32(ConfigManager.wbConfig.writeTimeout),
                NewLine = "\r\n"
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string rawData = _serialPort.ReadExisting(); // or ReadExisting()
                string weight = ParseWeight(rawData);

                // Update UI on main thread
                this.Invoke((MethodInvoker)delegate
                {
                    tbQtyMasuk.Text = weight;
                    //tbQtyTestTimbang.Text = weight;

                    _statusForm?.ShowSuccess(weight);
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    //tbQtyTestTimbang.Text = "Error " + ex.Message;

                    _statusForm?.Close();
                    MessageBox.Show($"Read error: {ex.Message}");
                });
            }
        }

        private string ParseWeight(string rawData)
        {

            rawData = rawData.Trim();

            // Method 1: Extract numeric value using regex
            var match = System.Text.RegularExpressions.Regex.Match(rawData, @"[\d]+\.?[\d]*");
            if (match.Success)
            {

                //return match.Value;
                if (double.TryParse(match.Value, out double weight))
                {
                    return weight.ToString("0.##"); // "50" or "50.5" (no trailing zeros)
                    //return weight.ToString("F2");
                }
            }

            return "0";
            //return rawData;
        }

        private void LoadDataKodeOokumenPabean()
        {
            cmbKodeDokPabean.Items.Clear();

            HANAConnetion hanaConn = new HANAConnetion();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_GET_KODE_DOKUMEN()";

                using (var command = new HanaCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbKodeDokPabean.Items.Add(reader["KODEDOKUMEN"]);
                        }
                    }
                }

                connection.Close();
            }
        }

        private void ShowReadingDialog(string type)
        {
            _statusForm = new WeighStatusForm();
            _statusForm.ShowReading();
            _statusForm.Show(this);
        }

        private void runningTime_Tick(object sender, EventArgs e)
        {
            lbDateTime.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
        }

        #region Weighbridge

        private void cbTMManual_CheckedChanged_1(object sender, EventArgs e)
        {
            if (cbTMManual.Checked)
            {
                timbangType = "M";
                tbQtyMasuk.Enabled = true;
                tbQtyMasuk.Text = Convert.ToString(0);
                btnTimbangMasuk.Visible = false;
            }
            else
            {
                timbangType = "A";
                tbQtyMasuk.Enabled = false;
                tbQtyMasuk.Text = Convert.ToString(0);
                btnTimbangMasuk.Visible = true;
            }
        }

        private void tbQtyMasuk_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, control characters (like backspace), and the decimal separator
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true; // Prevent the character from being entered
            }

            // Only allow one decimal point
            if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true; // Prevent multiple decimal points
            }
        }

        private void tbQtyKeluar_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, control characters (like backspace), and the decimal separator
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true; // Prevent the character from being entered
            }

            // Only allow one decimal point
            if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true; // Prevent multiple decimal points
            }
        }

        private void tbBaseDoc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Block the character input
            }
        }

        private void btnTimbangMasuk_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();

                _serialPort.DiscardInBuffer();

                // Show reading dialog
                ShowReadingDialog("Weigh In");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void btnTimbangKeluar_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();

                _serialPort.DiscardInBuffer();

                // Show reading dialog
                ShowReadingDialog("Weigh Out");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private string GenerateTiketNumber()
        {
            string tiketNum = "";

            string wbCategory = ConfigManager.wbConfig.prefix.ToString();

            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_GENERATE_TIKET_NUMBER('" + wbCategory + "')";

                using (var command = new HanaCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tiketNum = reader["TIKETNUM"].ToString();
                        }
                    }
                }

                connection.Close();
            }

            return tiketNum;
        }

        private void PrintWeighbridgeTicket(string ticketNumber)
        {
            ReportDocument reportDoc = new ReportDocument();

            try
            {
                reportDoc.Load(@"Assets\Report\WeighbridgeTiket.rpt");
                reportDoc.SetParameterValue("NOTIKET", ticketNumber);

                reportDoc.PrintToPrinter(1, false, 0, 0);

                MessageBox.Show($"Ticket {ticketNumber} sent to printer.", "Print Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to print ticket:\n{ex.Message}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                reportDoc.Close();
                reportDoc.Dispose();
            }
        }

        private void btnCariBaseDoc_Click(object sender, EventArgs e)
        {
            LoadDataComboBox();

            if (String.IsNullOrEmpty(tbBaseDoc.Text) && String.IsNullOrEmpty(tbTiketNum.Text))
            {
                MessageBox.Show("Base Document atau Nomor Tiket harus diisi.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!String.IsNullOrEmpty(tbBaseDoc.Text) && !String.IsNullOrEmpty(tbTiketNum.Text))
            {
                MessageBox.Show("Hanya bisa mencari dari Base Document atau Nomor Tiket.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (String.IsNullOrEmpty(cbDocType.Text) && !String.IsNullOrEmpty(tbBaseDoc.Text))
            {
                MessageBox.Show("Pilih Document Type terlebih dahulu", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (!String.IsNullOrEmpty(tbBaseDoc.Text))
                {
                    int docEntry;
                    string docType = cbDocType.SelectedItem.ToString();
                    string vendorRef = tbBaseDoc.Text;
                    string docNum = tbBaseSAP.Text;

                    if (String.IsNullOrEmpty(docType) || String.IsNullOrEmpty(vendorRef))
                    {
                        MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                        var connection = new HanaConnection(connectionString);

                        using (connection)
                        {
                            connection.Open();

                            string queryString2 = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC('" + docType + "','" + vendorRef + "')";

                            using (var command2 = new HanaCommand(queryString2, connection))
                            {
                                using (var reader2 = command2.ExecuteReader())
                                {
                                    if (!reader2.HasRows)
                                    {
                                        MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                        DisableFieldWB();
                                        ClearAllField();
                                    }
                                    else
                                    {
                                        while (reader2.Read())
                                        {
                                            tbTiketNum.Text = GenerateTiketNumber();
                                            tbTiketNum.Enabled = false;

                                            docEntry = Convert.ToInt32(reader2["DOCENTRY"]);
                                            docNum = reader2["DOCNUM"].ToString();
                                            vendorRef = reader2["VENDORREF"].ToString();
                                            noKontrak = reader2["KONTRAK"] != null ? reader2["KONTRAK"].ToString() : "";
                                            string itemCode = reader2["ITEMCODE"].ToString();
                                            int linenumSAP = Convert.ToInt32(reader2["LINENUM"]);

                                            tbCommodity.Text = itemCode;
                                            lbValDocEntSAP.Text = docEntry.ToString();
                                            lblValLineNumSAP.Text = linenumSAP.ToString();
                                            tbBaseSAP.Text = docNum;

                                            string tableHeader = docType == "PO" ? "OPOR" : "ORDR";
                                            string tableLine = docType == "PO" ? "POR1" : "RDR1";

                                            // Validasi Status dan Kuota Dokumen PO/SO langsung dari SAP
                                            try
                                            {
                                                string quotaQuery = $"SELECT T0.\"DocStatus\" AS HD_STATUS, T1.\"LineStatus\" AS LN_STATUS, T1.\"OpenQty\" AS OPEN_QTY, T1.\"Quantity\" AS TOTAL_QTY " +
                                                                    $"FROM \"{tableHeader}\" T0 INNER JOIN \"{tableLine}\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                                                    $"WHERE T0.\"DocEntry\" = {docEntry} AND T1.\"LineNum\" = {linenumSAP}";

                                                using (var cmdQuota = new HanaCommand(quotaQuery, connection))
                                                using (var readerQuota = cmdQuota.ExecuteReader())
                                                {
                                                    if (readerQuota.Read())
                                                    {
                                                        statusDocPO = readerQuota["HD_STATUS"].ToString();
                                                        string lineStatus = readerQuota["LN_STATUS"].ToString();
                                                        sisaKuotaPO = Convert.ToDouble(readerQuota["OPEN_QTY"]);
                                                        totalKuotaPO = Convert.ToDouble(readerQuota["TOTAL_QTY"]);

                                                        if (statusDocPO == "C" || lineStatus == "C")
                                                        {
                                                            MessageBox.Show($"Dokumen {docType} #{docNum} sudah berstatus CLOSED di SAP B1!\nDokumen ini tidak dapat diproses untuk penimbangan.",
                                                                "Dokumen Closed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                            DisableFieldWB();
                                                            ClearAllField();
                                                            return;
                                                        }

                                                        if (sisaKuotaPO <= 0)
                                                        {
                                                            MessageBox.Show($"Sisa kuota untuk {docType} #{docNum} sudah HABIS (Open Qty = 0)!\nDokumen ini tidak dapat diproses untuk penimbangan.",
                                                                "Kuota Habis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                                            DisableFieldWB();
                                                            ClearAllField();
                                                            return;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception exQuota)
                                            {
                                                System.Diagnostics.Debug.WriteLine("Cek Kuota Error: " + exQuota.Message);
                                            }

                                            // Penarikan otomatis Informasi Pabean dari Header Dokumen SAP (OPOR / ORDR)
                                            try
                                            {
                                                string pabeanQuery = $"SELECT * FROM \"{tableHeader}\" WHERE \"DocEntry\" = {docEntry}";
                                                using (var cmdPabean = new HanaCommand(pabeanQuery, connection))
                                                using (var rdrPabean = cmdPabean.ExecuteReader())
                                                {
                                                    if (rdrPabean.Read())
                                                    {
                                                        string GetColVal(params string[] columnNames)
                                                        {
                                                            for (int i = 0; i < columnNames.Length; i++)
                                                            {
                                                                string col = columnNames[i];
                                                                for (int c = 0; c < rdrPabean.FieldCount; c++)
                                                                {
                                                                    if (string.Equals(rdrPabean.GetName(c), col, StringComparison.OrdinalIgnoreCase))
                                                                    {
                                                                        if (!rdrPabean.IsDBNull(c))
                                                                        {
                                                                            string val = rdrPabean.GetValue(c)?.ToString()?.Trim();
                                                                            if (!string.IsNullOrEmpty(val)) return val;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            return string.Empty;
                                                        }

                                                        DateTime? GetColDate(params string[] columnNames)
                                                        {
                                                            for (int i = 0; i < columnNames.Length; i++)
                                                            {
                                                                string col = columnNames[i];
                                                                for (int c = 0; c < rdrPabean.FieldCount; c++)
                                                                {
                                                                    if (string.Equals(rdrPabean.GetName(c), col, StringComparison.OrdinalIgnoreCase))
                                                                    {
                                                                        if (!rdrPabean.IsDBNull(c))
                                                                        {
                                                                            if (DateTime.TryParse(rdrPabean.GetValue(c)?.ToString(), out DateTime dtVal))
                                                                            {
                                                                                return dtVal;
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            return null;
                                                        }

                                                        // 1. Nomor SPB
                                                        string spb = GetColVal("U_SOL_NOMOR_SPB", "U_SOL_NO_SPB", "SOL_NOMOR_SPB", "SOL_NO_SPB");
                                                        if (!string.IsNullOrEmpty(spb)) tbNomorSPB.Text = spb;

                                                        // 2. Tanggal SPB
                                                        DateTime? tglSpb = GetColDate("U_SOL_TANGGAL_SPB", "SOL_TANGGAL_SPB", "U_SOL_TGL_SPB");
                                                        if (tglSpb.HasValue)
                                                        {
                                                            dtTglSPB.Checked = true;
                                                            dtTglSPB.Value = tglSpb.Value;
                                                            dtTglSPB.CustomFormat = "yyyy-MM-dd";
                                                        }

                                                        // 3. Status Jalur
                                                        string jalur = GetColVal("U_SOL_STATUS_JALUR", "SOL_STATUS_JALUR", "U_STATUS_JALUR");
                                                        if (!string.IsNullOrEmpty(jalur)) cbStatusJalur.Text = jalur;

                                                        // 4. Fasilitas Pabean
                                                        string fasilitas = GetColVal("U_SOL_FASILITAS_PABEAN", "SOL_FASILITAS_PABEAN", "U_SOL_FASILITAS");
                                                        if (!string.IsNullOrEmpty(fasilitas)) cbFasilitasPabean.Text = fasilitas;

                                                        // 5. Kode Dokumen Pabean
                                                        string kdDok = GetColVal("U_SOL_KODE_DOKUMEN", "SOL_KODE_DOKUMEN");
                                                        if (!string.IsNullOrEmpty(kdDok)) cmbKodeDokPabean.Text = kdDok;

                                                        // 6. Nomor Dokumen Pabean (No Pendaftaran)
                                                        string noDok = GetColVal("U_SOL_NOMOR_DOKUMEN", "U_SOL_NO_DOKUMEN", "SOL_NOMOR_DOKUMEN");
                                                        if (!string.IsNullOrEmpty(noDok)) tbNoDokPabean.Text = noDok;

                                                        // 7. Tanggal Dokumen Pabean (Tanggal Pendaftaran)
                                                        DateTime? tglDok = GetColDate("U_SOL_TANGGAL_DOKUMEN", "SOL_TANGGAL_DOKUMEN", "U_SOL_TGL_DOKUMEN");
                                                        if (tglDok.HasValue)
                                                        {
                                                            dtTglDokPabean.Checked = true;
                                                            dtTglDokPabean.Value = tglDok.Value;
                                                            dtTglDokPabean.CustomFormat = "yyyy-MM-dd";
                                                        }

                                                        // 8. Nomor Aju
                                                        string noAju = GetColVal("U_SOL_NOMOR_AJU", "U_SOL_NO_AJU", "SOL_NOMOR_AJU");
                                                        if (!string.IsNullOrEmpty(noAju)) tbNomorAju.Text = noAju;
                                                    }
                                                }
                                            }
                                            catch (Exception exPabean)
                                            {
                                                System.Diagnostics.Debug.WriteLine("Pabean Auto-populate Error: " + exPabean.Message);
                                            }

                                            string infoMsg = $"Document SAP ditemukan.\n\n" +
                                                             $"No. Dokumen : {docNum}\n" +
                                                             (string.IsNullOrEmpty(noKontrak) ? "" : $"No. Kontrak : {noKontrak}\n") +
                                                             (totalKuotaPO > 0 ? $"Total Kuota : {totalKuotaPO:N2} MT\nSisa Kuota  : {sisaKuotaPO:N2} MT" : "");

                                            MessageBox.Show(infoMsg.Trim(), "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                            EnableFieldWB();

                                            btnCariBaseDoc.Visible = false;
                                            btnRefreshData.Visible = true;
                                        }
                                    }
                                }
                            }
                            connection.Close();
                        }
                    }
                }
                else
                {

                }
            }
        }

        private bool ValidasiDokumenPabean()
        {
            var val = Weighbridge.Core.Services.PabeanValidator.Validate(
                cmbKodeDokPabean.Text,
                tbNoDokPabean.Text,
                dtTglDokPabean.CustomFormat != " ",
                dtTglDokPabean.CustomFormat);

            if (!val.IsValid)
            {
                MessageBox.Show(val.ErrorMessage, "Validasi Dokumen Pabean", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void btnWBSave_Click(object sender, EventArgs e)
        {
            if (!ValidasiDokumenPabean()) return;

            double beratMasuk = 0;
            double.TryParse(tbQtyMasuk.Text, out beratMasuk);

            if (sisaKuotaPO > 0 && beratMasuk > 0 && beratMasuk > sisaKuotaPO)
            {
                DialogResult dr = MessageBox.Show(
                    $"PERHATIAN: Berat timbang masuk ({beratMasuk:N2} MT) MELEBIHI sisa kuota PO ({sisaKuotaPO:N2} MT)!\n\n" +
                    $"Jika dilanjutkan, dokumen ini akan memerlukan Split Kontrak saat Timbang Keluar.\n\n" +
                    $"Apakah Anda yakin ingin tetap melanjutkan?",
                    "Peringatan Kuota Terlampaui", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (dr != DialogResult.Yes)
                {
                    return;
                }
            }

            DialogResult result = MessageBox.Show("Pastikan semua data sudah benar. Lanjut simpan data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    string wbStatus = "WEIGHIN";
                    AddWBUDO(wbStatus);
                    PrintWeighbridgeTicket(tbTiketNum.Text);
                    //UpdatePO(docEntrySAP);

                    ClearAllField();
                    DisableFieldWB();
                    tbBaseDoc.Focus();
                    //tbTiketNum.Enabled = true;
                    btnCariBaseDoc.Visible = true;
                    btnRefreshData.Visible = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {

            DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {

                ClearAllField();
                DisableFieldWB();
                btnRefreshData.Visible = false;
                btnCariBaseDoc.Visible = true;
            }
        }

        private void EnableFieldWB()
        {
            tbNopol.Enabled = true;
            tbNamaSupir.Enabled = true;
            tbSIMSupir.Enabled = true;
            tbJnsTruck.Enabled = true;
            tbBrutoKebun.Enabled = true;
            tbTaraKebun.Enabled = true;
            tbNettoKebun.Enabled = true;
            tbFFAKebun.Enabled = true;
            tbMOISTKebun.Enabled = true;
            tbFFA.Enabled = true;
            tbMOIST.Enabled = true;
            tbIMP.Enabled = true;
            tbDOBI.Enabled = true;
            tbCAROTINE.Enabled = true;
            cbTMManual.Enabled = true;
            tbSealNo.Enabled = true;
            tbDONo.Enabled = true;
            tbNote.Enabled = true;
            cmbKodeDokPabean.Enabled = true;
            tbNoDokPabean.Enabled = true;
            dtTglDokPabean.Enabled = true;
            tbNomorSPB.Enabled = true;
            dtTglSPB.Enabled = true;
            cbStatusJalur.Enabled = true;
            cbFasilitasPabean.Enabled = true;
            tbNomorAju.Enabled = true;
            tbContNo1.Enabled = true;
            tbContNo2.Enabled = true;
            btnTimbangMasuk.Enabled = true;
            //btnSubmitUjiLab.Enabled = true;
            btnWBSave.Enabled = true;
            cbTransInt.Enabled = true;
            tbTransporterEks.Enabled = true;

        }

        private void DisableFieldWB()
        {
            //cbDocType.Enabled = false;
            //tbBaseDoc.Enabled = false;
            tbNopol.Enabled = false;
            tbNamaSupir.Enabled = false;
            tbSIMSupir.Enabled = false;
            tbJnsTruck.Enabled = false;
            tbBrutoKebun.Enabled = false;
            tbTaraKebun.Enabled = false;
            tbNettoKebun.Enabled = false;
            tbFFAKebun.Enabled = false;
            tbMOISTKebun.Enabled = false;
            tbFFA.Enabled = false;
            tbMOIST.Enabled = false;
            tbIMP.Enabled = false;
            tbDOBI.Enabled = false;
            tbCAROTINE.Enabled = false;
            cbTMManual.Enabled = false;
            btnTimbangMasuk.Enabled = false;
            //btnSubmitUjiLab.Enabled = false;
            btnWBSave.Enabled = false;
            tbNoDokPabean.Enabled = false;
            cmbKodeDokPabean.Enabled = false;
            dtTglDokPabean.Enabled = false;
            tbNomorSPB.Enabled = false;
            dtTglSPB.Enabled = false;
            cbStatusJalur.Enabled = false;
            cbFasilitasPabean.Enabled = false;
            tbNomorAju.Enabled = false;
            tbContNo1.Enabled = false;
            tbContNo2.Enabled = false;
            tbSealNo.Enabled = false;
            tbDONo.Enabled = false;
            tbNote.Enabled = false;
            cbTransInt.Enabled = false;
            tbTransporterEks.Enabled = false;
            //tbQtyMasuk.Enabled = false;
            //tbQtyKeluar.Enabled = false;
            //tbQtyNetto.Enabled = false;
        }

        private void ClearAllField()
        {
            lbValDocEntSAP.Text = String.Empty;
            //lblValDocNo.Text = String.Empty;
            //lblValKontrak.Text = String.Empty;
            cbDocType.Text = String.Empty;
            tbBaseDoc.Text = String.Empty;
            tbTiketNum.Text = String.Empty;
            tbNopol.Text = String.Empty;
            tbNamaSupir.Text = String.Empty;
            tbSIMSupir.Text = String.Empty;
            tbJnsTruck.Text = String.Empty;
            tbBrutoKebun.Text = String.Empty;
            tbTaraKebun.Text = String.Empty;
            tbNettoKebun.Text = String.Empty;
            tbFFAKebun.Text = String.Empty;
            tbMOISTKebun.Text = String.Empty;
            tbFFA.Text = String.Empty;
            tbMOIST.Text = String.Empty;
            tbDOBI.Text = String.Empty;
            tbIMP.Text = String.Empty;
            tbCAROTINE.Text = String.Empty;
            tbQtyMasuk.Text = "0";
            tbNoDokPabean.Text = String.Empty;
            cmbKodeDokPabean.Text = String.Empty;
            tbNomorSPB.Text = String.Empty;
            tbNomorAju.Text = String.Empty;
            tbContNo1.Text = String.Empty;
            tbContNo2.Text = String.Empty;
            tbSealNo.Text = String.Empty;
            tbDONo.Text = String.Empty;
            tbNote.Text = String.Empty;
            tbCommodity.Text = String.Empty;
            dtTglDokPabean.Value = DateTime.Now;
            dtTglDokPabean.Checked = false;
            dtTglDokPabean.CustomFormat = " ";
            dtTglSPB.Value = DateTime.Now;
            dtTglSPB.Checked = false;
            dtTglSPB.CustomFormat = " ";
            cbStatusJalur.SelectedIndex = -1;
            cbStatusJalur.Text = String.Empty;
            cbFasilitasPabean.SelectedIndex = -1;
            cbFasilitasPabean.Text = String.Empty;
            cbTMManual.Checked = false;
            cbTransInt.Text = String.Empty;
            tbTransporterEks.Text = String.Empty;
        }


        private void AddWBUDO(string wbStatus)
        {
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);
            var connection = new HanaConnection(connectionString);

            string code = "";

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_MAX_UDO_CODE()";

                using (var command = new HanaCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {

                        }
                        else
                        {
                            while (reader.Read())
                            {
                                code = reader["WBCODE"].ToString();
                            }
                        }
                    }
                }

                connection.Close();
            }

            try
            {
                string kodeDokPabean = "";

                if (cmbKodeDokPabean.SelectedIndex != -1)
                {
                    kodeDokPabean = cmbKodeDokPabean.Text.ToString();
                }

                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                //Declare all SAPbobsCOM untuk DI API UDO
                SAPbobsCOM.GeneralService oGeneralService;
                SAPbobsCOM.GeneralData oGeneralData;

                SAPbobsCOM.CompanyService oSTR = null;
                oSTR = oSBOConnection.oCompany.GetCompanyService();

                //Get a handle to the Stock Request UDO
                oGeneralService = oSTR.GetGeneralService("OWB");

                string transporterIntCode = cbTransInt.SelectedValue != null ? cbTransInt.SelectedValue.ToString() : "";

                //Specify data for main UDO (Header)
                oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
                oGeneralData.SetProperty("Code", code);
                oGeneralData.SetProperty("U_SOL_DOC_TYPE", cbDocType.SelectedItem.ToString());
                oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", tbBaseSAP.Text);
                oGeneralData.SetProperty("U_SOL_VENDOR_REF", tbBaseDoc.Text);
                oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lbValDocEntSAP.Text));
                oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
                oGeneralData.SetProperty("U_SOL_COMMODITY", tbCommodity.Text);
                oGeneralData.SetProperty("U_SOL_NO_KONTRAK", noKontrak);
                oGeneralData.SetProperty("U_SOL_NOPOL", tbNopol.Text);
                oGeneralData.SetProperty("U_SOL_NAMA_SUPIR", tbNamaSupir.Text);
                oGeneralData.SetProperty("U_SOL_NO_SIM_SUPIR", tbSIMSupir.Text);
                oGeneralData.SetProperty("U_SOL_JENIS_TRUCK", tbJnsTruck.Text);
                oGeneralData.SetProperty("U_SOL_BERAT_MASUK", Convert.ToDouble(tbQtyMasuk.Text));
                oGeneralData.SetProperty("U_SOL_BRUTO_KEBUN", tbBrutoKebun.Text);
                oGeneralData.SetProperty("U_SOL_TARA_KEBUN", tbTaraKebun.Text);
                oGeneralData.SetProperty("U_SOL_NETTO_KEBUN", tbNettoKebun.Text);
                oGeneralData.SetProperty("U_SOL_FFA_KEBUN", tbFFAKebun.Text);
                oGeneralData.SetProperty("U_SOL_MOIST_KEBUN", tbMOISTKebun.Text);
                oGeneralData.SetProperty("U_SOL_LAB_FFA", tbFFA.Text);
                oGeneralData.SetProperty("U_SOL_LAB_MOIST", tbMOIST.Text);
                oGeneralData.SetProperty("U_SOL_LAB_IMP", tbIMP.Text);
                oGeneralData.SetProperty("U_SOL_LAB_DOBI", tbDOBI.Text);
                oGeneralData.SetProperty("U_SOL_LAB_CAROTINE", tbCAROTINE.Text);
                oGeneralData.SetProperty("U_SOL_KODE_DOKUMEN", kodeDokPabean);
                oGeneralData.SetProperty("U_SOL_TANGGAL_DOKUMEN", dtTglDokPabean.Checked ? dtTglDokPabean.Value.ToString("yyyyMMdd") : "");
                oGeneralData.SetProperty("U_SOL_NO_DOKUMEN", tbNoDokPabean.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_SPB", tbNomorSPB.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_AJU", tbNomorAju.Text);

                try
                {
                    if (dtTglSPB.Checked)
                    {
                        oGeneralData.SetProperty("U_SOL_TANGGAL_SPB", Convert.ToDateTime(dtTglSPB.Value));
                    }
                    else
                    {
                        oGeneralData.SetProperty("U_SOL_TANGGAL_SPB", "");
                    }
                }
                catch { }

                try
                {
                    oGeneralData.SetProperty("U_SOL_STATUS_JALUR", cbStatusJalur.Text);
                }
                catch { }

                try
                {
                    oGeneralData.SetProperty("U_SOL_FASILITAS_PABEAN", cbFasilitasPabean.Text);
                }
                catch { }
                oGeneralData.SetProperty("U_SOL_TANGGAL_MASUK", Convert.ToDateTime(DateTime.Today));
                oGeneralData.SetProperty("U_SOL_WAKTU_MASUK", DateTime.Now);
                oGeneralData.SetProperty("U_SOL_SEAL_NO", tbSealNo.Text);
                oGeneralData.SetProperty("U_SOL_DO_NO", tbDONo.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_1_NO", tbContNo1.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_2_NO", tbContNo2.Text);
                oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", transporterIntCode);
                oGeneralData.SetProperty("U_SOL_TRANSPORTER_EKS", tbTransporterEks.Text);
                oGeneralData.SetProperty("U_SOL_NOTE", tbNote.Text);
                oGeneralData.SetProperty("U_SOL_USER", lblUsername.Text);
                oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
                oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumSAP.Text));

                if (!String.IsNullOrEmpty(transporterIntCode))
                {
                    oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", transporterIntCode);
                    oGeneralData.SetProperty("U_SOL_TRANSPORTER_EKS", "");
                }
                else if (!String.IsNullOrEmpty(tbTransporterEks.Text))
                {
                    oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", "");
                    oGeneralData.SetProperty("U_SOL_TRANSPORTER_EKS", tbTransporterEks.Text);
                }

                if (dtTglDokPabean.Checked)
                {
                    oGeneralData.SetProperty("U_SOL_TANGGAL_DOKUMEN", Convert.ToDateTime(dtTglDokPabean.Value));
                }
                else
                {
                    oGeneralData.SetProperty("U_SOL_TANGGAL_DOKUMEN", "");
                }

                //Add records
                oGeneralService.Add(oGeneralData);

                oSBOConnection.oCompany.Disconnect();

                MessageBox.Show("Data added. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                oSBOConnection.oCompany.Disconnect();
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //private void UpdatePO(int docEntrySAP)
        //{
        //    modGlobal = global();

        //    try
        //    {
        //        oSBOConnection.connectSBO(global());

        //        SAPbobsCOM.Documents oPO = null;
        //        oPO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oPurchaseOrders);

        //        oPO.GetByKey(docEntrySAP);

        //        oPO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;

        //        oPO.Update();

        //        oSBOConnection.oCompany.Disconnect();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        //private void UpdateDO(int docEntryDOSAP, int lineNumDOSAP, double qtyTimbangDO)
        //{
        //    modGlobal = global();

        //    try
        //    {
        //        oSBOConnection.connectSBO(global());

        //        SAPbobsCOM.Documents oDO = null;
        //        oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

        //        oDO.GetByKey(docEntryDOSAP);

        //        oDO.Lines.SetCurrentLine(Convert.ToInt32(lineNumDOSAP));
        //        oDO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyTimbangDO;

        //        int retval = 0;

        //        retval = oDO.Update();

        //        if (retval == 0)
        //        {
        //            oSBOConnection.oCompany.Disconnect();
        //            MessageBox.Show("Data Updated to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //            ClearAllFieldUpdateDO();
        //        }
        //        else
        //        {
        //            oSBOConnection.oCompany.Disconnect();
        //            MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //            ClearAllFieldUpdateDO();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        oSBOConnection.oCompany.Disconnect();
        //        MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //        ClearAllFieldUpdateDO();
        //    }
        //}

        #endregion

        #region Function

        private void LoadDataComboBox()
        {
            HANAConnetion hanaConn = new HANAConnetion();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            string query = "SELECT \"CardCode\", \"CardName\" FROM \"OCRD\" WHERE \"GroupCode\" = 103";

            using (HanaConnection conn = new HanaConnection(connectionString))
            {
                using (HanaCommand cmd = new HanaCommand(query, conn))
                {
                    HanaDataAdapter da = new HanaDataAdapter(cmd);
                    DataTable dt = new DataTable();

                    try
                    {
                        conn.Open();
                        da.Fill(dt);

                        cbTransInt.DisplayMember = "CardName";
                        cbTransInt.ValueMember = "CardCode";

                        cbTransInt.DataSource = dt;

                        cbTransInt.SelectedIndex = -1;
                        cbTransInt.Text = ""; 
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Terjadi kesalahan saat memuat data: " + ex.Message);
                    }
                }
            }
        }

        private void cbTransInt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbTransInt.SelectedValue != null && cbTransInt.SelectedValue is string)
            {
                string selectedCardCode = cbTransInt.SelectedValue.ToString();

            }
        }

        #endregion

        private void tbBaseDoc_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
