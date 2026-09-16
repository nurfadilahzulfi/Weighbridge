using CrystalDecisions.CrystalReports.Engine;
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
    public partial class TimbangKeluarForm : Form
    {
        private SerialPort _serialPort;
        private WeighStatusForm _statusForm;

        HANAConnetion hanaConn = new HANAConnetion();
        SBOConnection oSBOConnection = new SBOConnection();

        public string timbangType; //A = Auto, M = Manual
        private string noKontrak = "";

        public TimbangKeluarForm()
        {
            InitializeComponent();
        }

        private void TImbangKeluarForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataKodeOokumenPabean();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;

            dtTglDokPabean.ShowCheckBox = true;
            dtTglDokPabean.Checked = false;
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
                    tbQtyKeluar.Text = weight;
                    //tbQtyTestTimbang.Text = weight;

                    // Update dialog → show success → auto close
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
                // Parse as double to handle both "0000050" and "0000050.50"
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

        private void LoadDataNomorTangki(string itemName)
        {
            cmbTangkiNo.Items.Clear();

            HANAConnetion hanaConn = new HANAConnetion();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = $"CALL SOL_SP_ADDON_WB_GET_NOMOR_TANGKI('{itemName}')";

                using (var command = new HanaCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbTangkiNo.Items.Add(reader["NOMORTANGKI"]);
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
            _statusForm.Show(this);             // non-blocking, keeps serial reading active
        }

        private void runningTime_Tick(object sender, EventArgs e)
        {
            lbDateTime.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
        }

        
        private void cbTKManual_CheckedChanged(object sender, EventArgs e)
        {
            if (cbTKManual.Checked)
            {
                timbangType = "M";
                tbQtyKeluar.Enabled = true;
                tbQtyKeluar.Text = Convert.ToString(0);
                btnTimbangKeluar.Visible = false;
            }
            else
            {
                timbangType = "A";
                tbQtyKeluar.Enabled = false;
                tbQtyKeluar.Text = Convert.ToString(0);
                btnTimbangKeluar.Visible = true;
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


        private void tbQtyKeluar_TextChanged(object sender, EventArgs e)
        {
            double qtyNetto;

            if (Double.TryParse(tbQtyMasuk.Text, out double qtyMasuk) && Double.TryParse(tbQtyKeluar.Text, out double qtyKeluar))
            {

                if (qtyMasuk > qtyKeluar)
                {
                    qtyNetto = qtyMasuk - qtyKeluar;

                    tbQtyNetto.Text = qtyNetto.ToString();
                }
                else
                {
                    qtyNetto = qtyKeluar - qtyMasuk;

                    tbQtyNetto.Text = qtyNetto.ToString();
                }
            }
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

        private DataTable searchByBaseDoc(string docType, string docNum)
        {
            DataTable dt = new DataTable();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_JOIN_WB_DATA('" + docType + "','" + docNum + "')";

                using (var command = new HanaCommand(queryString, connection))
                {
                    HanaDataAdapter adapter = new HanaDataAdapter(command);
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        private void btnCariBaseDoc_Click(object sender, EventArgs e)
        {
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
                    string docType = cbDocType.SelectedItem.ToString();
                    string docNum = tbBaseDoc.Text;

                    if (String.IsNullOrEmpty(docType) || String.IsNullOrEmpty(docNum))
                    {
                        MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        DataTable result = searchByBaseDoc(docType, docNum);

                        if (result == null || result.Rows.Count == 0)
                        {
                            MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            DisableFieldWB();
                            ClearAllField();
                        }
                        else if (result.Rows.Count == 1)
                        {
                            MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            fillFormFromDataRow(result.Rows[0]);

                            EnableFieldWB();
                            btnCariBaseDoc.Visible = false;
                            btnRefreshData.Visible = true;
                            lblStatusWB.Visible = true;
                            lblValStatusWB.Visible = true;
                            tbTiketNum.Enabled = false; 
                            LoadDataNomorTangki(tbCommodity.Text);
                        }
                        else
                        {
                            MessageBox.Show($"Document SAP ditemukan. Total {result.Rows.Count} tiket. Pilih salah satu tiket.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            using (SelectTicketForm selectTicketForm = new SelectTicketForm(result))
                            {
                                if(selectTicketForm.ShowDialog(this) == DialogResult.OK && selectTicketForm.SelectedTicket != null)
                                {
                                    fillFormFromDataRow(selectTicketForm.SelectedTicket);

                                    EnableFieldWB();
                                    btnCariBaseDoc.Visible = false;
                                    btnRefreshData.Visible = true;
                                    lblStatusWB.Visible = true;
                                    lblValStatusWB.Visible = true; 
                                    tbTiketNum.Enabled = false;
                                    LoadDataNomorTangki(tbCommodity.Text);
                                }
                            }
                        }
                    }
                }
                else
                {
                    string noTiket = tbTiketNum.Text;
                    string noKontrak;

                    string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                    var connection = new HanaConnection(connectionString);

                    using (connection)
                    {
                        connection.Open();

                        string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_NO_TIKET('" + noTiket + "')";

                        using (var command = new HanaCommand(queryString, connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                if (!reader.HasRows)
                                {
                                    MessageBox.Show("Nomor Tiket tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                    DisableFieldWB();
                                    ClearAllField();
                                }
                                else
                                {
                                    while (reader.Read())
                                    {
                                        MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        tbTiketNum.Enabled = false;

                                        if (reader["U_SOL_TANGGAL_DOKUMEN"] == DBNull.Value)
                                        {
                                            dtTglDokPabean.CustomFormat = " ";
                                        }
                                        else
                                        {
                                            dtTglDokPabean.Value = Convert.ToDateTime(reader["U_SOL_TANGGAL_DOKUMEN"]);
                                        }

                                        if(reader["U_SOL_KODE_DOKUMEN"] == DBNull.Value)
                                        {
                                            LoadDataKodeOokumenPabean();
                                        }
                                        else
                                        {
                                            cmbKodeDokPabean.Text = reader["U_SOL_KODE_DOKUMEN"].ToString();
                                        }

                                        if (reader["U_SOL_NOMOR_TANGKI"] == DBNull.Value)
                                        {
                                            LoadDataNomorTangki(tbCommodity.Text);
                                        }
                                        else
                                        {
                                            cmbTangkiNo.Text = reader["U_SOL_NOMOR_TANGKI"].ToString();
                                        }

                                        cbDocType.Text = reader["U_SOL_DOC_TYPE"].ToString();
                                        tbBaseSAP.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
                                        //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
                                        tbBaseDoc.Text = reader["U_SOL_VENDOR_REF"].ToString();
                                        lbValDocEntSAP.Text = reader["U_SOL_DOC_ENTRY_SAP"].ToString();
                                        lblValLineNumSAP.Text = reader["U_SOL_LINENUM_SAP"].ToString();
                                        lblValStatusWB.Text = reader["U_SOL_STATUS"].ToString();
                                         if (reader["U_SOL_NO_KONTRAK"] != DBNull.Value)
                                         {
                                             noKontrak = reader["U_SOL_NO_KONTRAK"].ToString();
                                         }
                                        tbNopol.Text = reader["U_SOL_NOPOL"].ToString();
                                        tbNamaSupir.Text = reader["U_SOL_NAMA_SUPIR"].ToString();
                                        tbSIMSupir.Text = reader["U_SOL_NO_SIM_SUPIR"].ToString();
                                        tbJnsTruck.Text = reader["U_SOL_JENIS_TRUCK"].ToString();
                                        tbBrutoKebun.Text = Convert.ToDecimal(reader["U_SOL_BRUTO_KEBUN"]).ToString("0.##");
                                        tbTaraKebun.Text = Convert.ToDecimal(reader["U_SOL_TARA_KEBUN"]).ToString("0.##");
                                        tbNettoKebun.Text = Convert.ToDecimal(reader["U_SOL_NETTO_KEBUN"]).ToString("0.##");
                                        tbFFAKebun.Text = reader["U_SOL_FFA_KEBUN"].ToString();
                                        tbMOISTKebun.Text = reader["U_SOL_MOIST_KEBUN"].ToString();
                                        tbFFA.Text = reader["U_SOL_LAB_FFA"].ToString();
                                        tbMOIST.Text = reader["U_SOL_LAB_MOIST"].ToString();
                                        tbDOBI.Text = reader["U_SOL_LAB_IMP"].ToString();
                                        tbIMP.Text = reader["U_SOL_LAB_DOBI"].ToString();
                                        tbCAROTINE.Text = reader["U_SOL_LAB_CAROTINE"].ToString();
                                        tbCommodity.Text = reader["U_SOL_COMMODITY"].ToString();
                                        tbSealNo.Text = reader["U_SOL_SEAL_NO"].ToString();
                                        tbDONo.Text = reader["U_SOL_DO_NO"].ToString();
                                        tbNote.Text = reader["U_SOL_NOTE"].ToString();
                                        tbNoDokPabean.Text = reader["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
                                        tbNomorSPB.Text = reader["U_SOL_NOMOR_SPB"].ToString();
                                        tbNomorAju.Text = reader["U_SOL_NOMOR_AJU"].ToString();
                                        tbContNo1.Text = reader["U_SOL_CONTAINER_1_NO"].ToString();
                                        tbContNo2.Text = reader["U_SOL_CONTAINER_2_NO"].ToString();
                                        tbQtyMasuk.Text = Convert.ToDecimal(reader["U_SOL_BERAT_MASUK"]).ToString("0.##");
                                        tbQtyKeluar.Text = Convert.ToDecimal(reader["U_SOL_BERAT_KELUAR"]).ToString("0.##");
                                        tbQtyNetto.Text = Convert.ToDecimal(reader["U_SOL_BERAT_NETTO"]).ToString("0.##");
                                        valTanggalMasuk.Text = reader["U_SOL_TANGGAL_MASUK"].ToString();
                                        valWaktuMasuk.Text = reader["U_SOL_WAKTU_MASUK"].ToString();
                                        tbTransporterInt.Text = reader["U_SOL_TRANSPORTER_INT"].ToString();
                                        tbTransporterEks.Text = reader["U_SOL_TRANSPORTER_EKS"].ToString();

                                        EnableFieldWB();
                                        btnCariBaseDoc.Visible = false;
                                        btnRefreshData.Visible = true;
                                        lblStatusWB.Visible = true;
                                        lblValStatusWB.Visible = true;

                                        LoadDataNomorTangki(tbCommodity.Text);
                                    }
                                }
                            }
                        }

                        connection.Close();
                    }
                }
            }

            tbTransporterInt.Enabled = false;
        }

        private void fillFormFromDataRow(DataRow row)
        {

            int docEntry = Convert.ToInt32(row["U_SOL_DOC_ENTRY_SAP"]);
            string docNum = row["U_SOL_VENDOR_REF"].ToString();
            string noKontrak = row["U_SOL_NO_KONTRAK"].ToString();
            int linenumSAP = Convert.ToInt32(row["U_SOL_LINENUM_SAP"]);

            if (row["U_SOL_TANGGAL_DOKUMEN"] == DBNull.Value)
            {
                dtTglDokPabean.Checked = false;
                dtTglDokPabean.CustomFormat = " ";
            }
            else
            {
                dtTglDokPabean.Value = Convert.ToDateTime(row["U_SOL_TANGGAL_DOKUMEN"]);
            }

            if (row["U_SOL_KODE_DOKUMEN"] == DBNull.Value)
            {
                LoadDataKodeOokumenPabean();
            }
            else
            {
                cmbKodeDokPabean.Text = row["U_SOL_KODE_DOKUMEN"].ToString();
            }

            if (row["U_SOL_NOMOR_TANGKI"] == DBNull.Value)
            {
                LoadDataNomorTangki(tbCommodity.Text);
            }
            else
            {
                cmbTangkiNo.Text = row["U_SOL_NOMOR_TANGKI"].ToString();
            }

            //lblValDocNo.Text = ": " + docNum;
            //lblValKontrak.Text = ": " + noKontrak;
            cbDocType.Text = row["U_SOL_DOC_TYPE"].ToString();
            tbBaseDoc.Text = row["U_SOL_VENDOR_REF"].ToString();
            tbTiketNum.Text = row["U_SOL_TIKET_WB"].ToString();
            if (row["U_SOL_NO_KONTRAK"] != DBNull.Value)
            {
                noKontrak = row["U_SOL_NO_KONTRAK"].ToString();
            }
            lblValStatusWB.Text = row["U_SOL_STATUS"].ToString();
            tbNopol.Text = row["U_SOL_NOPOL"].ToString();
            tbNamaSupir.Text = row["U_SOL_NAMA_SUPIR"].ToString();
            tbSIMSupir.Text = row["U_SOL_NO_SIM_SUPIR"].ToString();
            tbJnsTruck.Text = row["U_SOL_JENIS_TRUCK"].ToString();
            tbBrutoKebun.Text = Convert.ToDecimal(row["U_SOL_BRUTO_KEBUN"]).ToString("0.##");
            tbTaraKebun.Text = Convert.ToDecimal(row["U_SOL_TARA_KEBUN"]).ToString("0.##");
            tbNettoKebun.Text = Convert.ToDecimal(row["U_SOL_NETTO_KEBUN"]).ToString("0.##");
            tbFFAKebun.Text = row["U_SOL_FFA_KEBUN"].ToString();
            tbMOISTKebun.Text = row["U_SOL_MOIST_KEBUN"].ToString();
            tbFFA.Text = row["U_SOL_LAB_FFA"].ToString();
            tbMOIST.Text = row["U_SOL_LAB_MOIST"].ToString();
            tbDOBI.Text = row["U_SOL_LAB_IMP"].ToString();
            tbIMP.Text = row["U_SOL_LAB_DOBI"].ToString();
            tbCAROTINE.Text = row["U_SOL_LAB_CAROTINE"].ToString();
            tbCommodity.Text = row["U_SOL_COMMODITY"].ToString();
            tbSealNo.Text = row["U_SOL_SEAL_NO"].ToString();
            tbDONo.Text = row["U_SOL_DO_NO"].ToString();
            tbNote.Text = row["U_SOL_NOTE"].ToString();
            //cmbKodeDokPabean.SelectedItem = row["U_SOL_KODE_DOKUMEN"].ToString();
            tbNoDokPabean.Text = row["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
            tbNomorSPB.Text = row["U_SOL_NOMOR_SPB"].ToString();
            tbNomorAju.Text = row["U_SOL_NOMOR_AJU"].ToString();
            tbContNo1.Text = row["U_SOL_CONTAINER_1_NO"].ToString();    
            tbContNo2.Text = row["U_SOL_CONTAINER_2_NO"].ToString();
            
            tbQtyMasuk.Text = Convert.ToDecimal(row["U_SOL_BERAT_MASUK"]).ToString("0.##");
            tbQtyKeluar.Text = Convert.ToDecimal(row["U_SOL_BERAT_KELUAR"]).ToString("0.##");
            tbQtyNetto.Text = Convert.ToDecimal(row["U_SOL_BERAT_NETTO"]).ToString("0.##");
            lbValDocEntSAP.Text = docEntry.ToString();
            lblValLineNumSAP.Text = linenumSAP.ToString();
            valTanggalMasuk.Text = row["U_SOL_TANGGAL_MASUK"].ToString();
            valWaktuMasuk.Text = row["U_SOL_WAKTU_MASUK"].ToString();
            tbTransporterInt.Text = row["U_SOL_TRANSPORTER_INT"].ToString();
            tbTransporterEks.Text = row["U_SOL_TRANSPORTER_EKS"].ToString();
        }

        private bool ValidasiDokumenPabean()
        {
            bool adaKode = !string.IsNullOrWhiteSpace(cmbKodeDokPabean.Text) && cmbKodeDokPabean.SelectedIndex != -1;
            bool adaNomor = !string.IsNullOrWhiteSpace(tbNoDokPabean.Text);
            bool adaTanggal = dtTglDokPabean.Checked;

            if (adaKode)
            {
                if (!adaNomor)
                {
                    MessageBox.Show("Nomor Dokumen Pabean wajib diisi jika Kode Dokumen dipilih!",
                        "Validasi Dokumen Pabean", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tbNoDokPabean.Focus();
                    return false;
                }

                if (!adaTanggal)
                {
                    MessageBox.Show("Tanggal Dokumen Pabean wajib diisi (centang tanggal) jika Kode Dokumen dipilih!",
                        "Validasi Dokumen Pabean", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtTglDokPabean.Focus();
                    return false;
                }
            }
            else if (adaNomor)
            {
                MessageBox.Show("Kode Dokumen Pabean wajib dipilih jika Nomor Dokumen diisi!",
                    "Validasi Dokumen Pabean", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbKodeDokPabean.Focus();
                return false;
            }

            return true;
        }

        private void btnWBSave_Click(object sender, EventArgs e)
        {
            if (!ValidasiDokumenPabean()) return;

            DialogResult result = MessageBox.Show("Pastikan semua data sudah benar. Lanjut simpan data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                string tiketNum = tbTiketNum.Text;
                try
                {
                    if (cbDocType.SelectedItem.ToString() == "PO")
                    {
                        SyncGRPO();
                        PrintWeighbridgeTicket(tiketNum);
                    }
                    else
                    {
                        int docEntry = Convert.ToInt32(lbValDocEntSAP.Text);
                        int linenumSAP = Convert.ToInt32(lblValLineNumSAP.Text);

                        string itemCode = "";
                        string whsCode = "";

                        string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                        var connection = new HanaConnection(connectionString);

                        using (connection)
                        {
                            connection.Open();

                            string queryString = $"CALL SOL_SP_ADDON_WB_GET_ITEMCODE_WHSCODE_RDR1({docEntry}, {linenumSAP})";

                            using (var command = new HanaCommand(queryString, connection))
                            {
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        itemCode = reader["ITEMCODE"].ToString();
                                        whsCode = reader["WHSCODE"].ToString();
                                    }
                                }
                            }
                        }
                        connection.Close();

                        SyncDO(itemCode, whsCode);
                        PrintWeighbridgeTicket(tiketNum);
                    }

                    ClearAllField();
                    DisableFieldWB();
                    tbBaseDoc.Focus();
                    btnCariBaseDoc.Visible = true;
                    btnRefreshData.Visible = false;

                    tbTiketNum.Enabled = true;
                    tbQtyKeluar.Enabled = false;
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
                lblStatusWB.Visible = false;
                lblValStatusWB.Visible = false;
                tbTiketNum.Enabled = true;
            }
        }

        private void EnableFieldWB()
        {
            if (lblValStatusWB.Text == "WEIGHIN")
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
                cbTKManual.Enabled = true;
                tbSealNo.Enabled = true;
                tbDONo.Enabled = true;
                tbNote.Enabled = true;
                cmbKodeDokPabean.Enabled = true;
                tbNoDokPabean.Enabled = true;
                dtTglDokPabean.Enabled = true;
                tbNomorSPB.Enabled = true;
                tbNomorAju.Enabled = true;
                tbContNo1.Enabled = true;
                tbContNo2.Enabled = true;
                btnTimbangKeluar.Enabled = true;
                //btnSubmitUjiLab.Enabled = true;
                btnWBSave.Enabled = true;
                tbTransporterInt.Enabled = true;
                tbTransporterEks.Enabled = true;
                cmbTangkiNo.Enabled = true;
            }
            else if (lblValStatusWB.Text == "WEIGHOUT" || lblValStatusWB.Text == "SYNCSAP")
            {
                DisableFieldWB();
            }
            else
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
                cbTKManual.Enabled = false;
                tbSealNo.Enabled = true;
                tbDONo.Enabled = true;
                tbNote.Enabled = true;
                cmbKodeDokPabean.Enabled = true;
                tbNoDokPabean.Enabled = true;
                dtTglDokPabean.Enabled = true;
                tbNomorSPB.Enabled = true;
                tbNomorAju.Enabled = true;
                tbContNo1.Enabled = true;
                tbContNo2.Enabled = true;
                btnTimbangKeluar.Enabled = false;
                //btnSubmitUjiLab.Enabled = true;
                btnWBSave.Enabled = true;
                //tbQtyMasuk.Enabled = true;
                //tbQtyKeluar.Enabled = true;
                //tbQtyNetto.Enabled = true;
                tbTransporterInt.Enabled = true;
                tbTransporterEks.Enabled = true;
                cmbTangkiNo.Enabled = true;
            }

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
            cbTKManual.Enabled = false;
            cbTKManual.Checked = false;
            btnTimbangKeluar.Enabled = false;
            //btnSubmitUjiLab.Enabled = false;
            btnWBSave.Enabled = false;
            tbNoDokPabean.Enabled = false;
            cmbKodeDokPabean.Enabled = false;
            dtTglDokPabean.Enabled = false;
            tbNomorSPB.Enabled = false;
            tbNomorAju.Enabled = false;
            tbContNo1.Enabled = false;
            tbContNo2.Enabled = false;
            tbSealNo.Enabled = false;
            tbDONo.Enabled = false;
            tbNote.Enabled = false;
            tbTransporterInt.Enabled = false;
            tbTransporterEks.Enabled = false;
            cmbTangkiNo.Enabled = false;
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
            tbQtyKeluar.Text = "0";
            tbQtyNetto.Text = "0";
            lblValStatusWB.Text = String.Empty;
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
            cbTKManual.Checked = false;
            tbTransporterInt.Text = String.Empty;
            tbTransporterEks.Text = String.Empty;
            cmbTangkiNo.Text = String.Empty;
        }

        #region Sync

        private void SyncGRPO()
        {
            int docEntryPO = Convert.ToInt32(lbValDocEntSAP.Text);
            int baseLinePO = Convert.ToInt32(lblValLineNumSAP.Text);
            string docType = cbDocType.Text;
            string isIns = "";
            int baseType = 0;
            int docEntryReserve = 0;
            int baseLineReserve = 0;

            try
            {
                string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                var connection = new HanaConnection(connectionString);

                using (connection)
                {
                    connection.Open();

                    string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_PO_SO_RESERVE('{docType}', {docEntryPO})";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                isIns = reader["ISINS"].ToString();
                                baseType = Convert.ToInt32(reader["BASETYPE"]);
                                docEntryReserve = Convert.ToInt32(reader["DOCENTRY"]);
                                baseLineReserve = Convert.ToInt32(reader["LINENUM"]);
                            }
                        }
                    }
                }
                connection.Close();

                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                oSBOConnection.oCompany.StartTransaction();

                //memaksa relasi 1 Truk = 1 Tiket Timbang = 1 Dokumen Transaksi SAP.
                Documents oGRPO = null;
                oGRPO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                //No Polisi, Nama Supir, No Tiket, No SPB, dan No Aju Pabean 
                // ditaruh di HEADER dokumen SAP (OPDN / ODLN), bukan di baris/detail.
                oGRPO.DocDate = DateTime.Today;
                oGRPO.UserFields.Fields.Item("U_SOL_KODE_DOKUMEN").Value = cmbKodeDokPabean.Text?.Length >= 7 ? cmbKodeDokPabean.Text.ToString().Substring(0, 7) : string.Empty;
                oGRPO.UserFields.Fields.Item("U_SOL_NOMOR_DOKUMEN").Value = tbNoDokPabean.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_TANGGAL_DOKUMEN").Value = dtTglDokPabean.Value;
                oGRPO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_NO_SPB").Value = tbNomorSPB.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_NOMOR_AJU").Value = tbNomorAju.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_NAMA_SUPIR").Value = tbNamaSupir.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_JENIS_TRUCK").Value = tbJnsTruck.Text;
                oGRPO.UserFields.Fields.Item("U_SOL_NO_PLAT").Value = tbNopol.Text;

                if (!String.IsNullOrEmpty(tbTransporterInt.Text))
                {
                    oGRPO.UserFields.Fields.Item("U_SOL_VENDOR").Value = tbTransporterInt.Text;
                }
                else if (!String.IsNullOrEmpty(tbTransporterEks.Text))
                {
                    oGRPO.UserFields.Fields.Item("U_SOL_NAMA_TRANSPORTER").Value = tbTransporterEks.Text;
                }

                //oGRPO.Comments = "Generated by Weighbridge Addon.";

                if (isIns == "Y" && baseType == 22)
                {
                    oGRPO.Lines.BaseType = 18;
                    oGRPO.Lines.BaseEntry = docEntryReserve;
                    oGRPO.Lines.BaseLine = baseLineReserve;
                    //oGRPO.Lines.ItemCode = grpodetail.ItemCode;
                    //oGRPO.Lines.ItemDescription = grpodetail.ItemName;
                    //oGRPO.Lines.Quantity = Convert.ToDouble(tbQtyNetto.Text);
                    oGRPO.Lines.InventoryQuantity = Convert.ToDouble(tbQtyNetto.Text);
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = tbBrutoKebun.Text;
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = tbTaraKebun.Text;
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = tbNettoKebun.Text;
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = tbFFAKebun.Text;
                    oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = tbMOISTKebun.Text;
                    oGRPO.Lines.WarehouseCode = cmbTangkiNo.Text;

                    oGRPO.Lines.BatchNumbers.BatchNumber = tbTiketNum.Text;
                    oGRPO.Lines.BatchNumbers.Quantity = Convert.ToDouble(tbQtyNetto.Text);
                    //oGRPO.Lines.BatchNumbers.ExpiryDate = new DateTime(2025, 12, 31);
                    oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;

                    oGRPO.Lines.BatchNumbers.Add();

                    oGRPO.Lines.Add();
                }
                else
                {
                    double qtyNettoTotal = Convert.ToDouble(tbQtyNetto.Text);
                    string contractNo = noKontrak.Trim();
                    double baseOpenQty = 0;

                    try
                    {
                        using (var connCheck = new HanaConnection(connectionString))
                        {
                            connCheck.Open();
                            string checkBaseQtyQuery = $"SELECT T1.\"OpenQty\" FROM \"POR1\" T1 WHERE T1.\"DocEntry\" = {docEntryPO} AND T1.\"LineNum\" = {baseLinePO}";
                            using (var cmd = new HanaCommand(checkBaseQtyQuery, connCheck))
                            using (var rdr = cmd.ExecuteReader())
                            {
                                if (rdr.Read() && rdr["OpenQty"] != DBNull.Value)
                                {
                                    baseOpenQty = Convert.ToDouble(rdr["OpenQty"]);
                                }
                            }
                            connCheck.Close();
                        }
                    }
                    catch (Exception exHana)
                    {
                        System.Diagnostics.Debug.WriteLine("Cek Base OpenQty Error: " + exHana.Message);
                    }

                    // Jika kuota PO utama mencukupi atau tidak ada nomor kontrak, gunakan single-line biasa
                    if (baseOpenQty <= 0 || qtyNettoTotal <= baseOpenQty || string.IsNullOrEmpty(contractNo))
                    {
                        oGRPO.Lines.BaseType = 22;
                        oGRPO.Lines.BaseEntry = docEntryPO;
                        oGRPO.Lines.BaseLine = baseLinePO;
                        oGRPO.Lines.InventoryQuantity = qtyNettoTotal;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = tbBrutoKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = tbTaraKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = tbNettoKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = tbFFAKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = tbMOISTKebun.Text;
                        oGRPO.Lines.WarehouseCode = cmbTangkiNo.Text;

                        oGRPO.Lines.BatchNumbers.BatchNumber = tbTiketNum.Text;
                        oGRPO.Lines.BatchNumbers.Quantity = qtyNettoTotal;
                        oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                        oGRPO.Lines.BatchNumbers.Add();

                        oGRPO.Lines.Add();
                    }
                    else
                    {
                        // SPLIT KONTRAK OTOMATIS: Netto melebihi sisa PO pertama
                        // Line 1: Isi penuh sesuai sisa kuota PO pertama
                        oGRPO.Lines.BaseType = 22;
                        oGRPO.Lines.BaseEntry = docEntryPO;
                        oGRPO.Lines.BaseLine = baseLinePO;
                        oGRPO.Lines.InventoryQuantity = baseOpenQty;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = tbBrutoKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = tbTaraKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = tbNettoKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = tbFFAKebun.Text;
                        oGRPO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = tbMOISTKebun.Text;
                        oGRPO.Lines.WarehouseCode = cmbTangkiNo.Text;

                        oGRPO.Lines.BatchNumbers.BatchNumber = tbTiketNum.Text;
                        oGRPO.Lines.BatchNumbers.Quantity = baseOpenQty;
                        oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                        oGRPO.Lines.BatchNumbers.Add();

                        oGRPO.Lines.Add();

                        double remainingQty = qtyNettoTotal - baseOpenQty;

                        // Line 2 dst: Alokasikan sisa muatan ke PO lain dalam kontrak yang sama
                        try
                        {
                            using (var connSplit = new HanaConnection(connectionString))
                            {
                                connSplit.Open();
                                string splitQuery = $"SELECT T0.\"DocEntry\", T0.\"DocNum\", T1.\"LineNum\", T1.\"OpenQty\" " +
                                                    $"FROM \"OPOR\" T0 INNER JOIN \"POR1\" T1 ON T0.\"DocEntry\" = T1.\"DocEntry\" " +
                                                    $"WHERE (T0.\"U_SOL_NO_KONTRAK\" = '{contractNo}' OR T0.\"NumAtCard\" = '{contractNo}') " +
                                                    $"  AND T0.\"DocStatus\" = 'O' AND T1.\"LineStatus\" = 'O' AND T1.\"OpenQty\" > 0 " +
                                                    $"  AND NOT (T0.\"DocEntry\" = {docEntryPO} AND T1.\"LineNum\" = {baseLinePO}) " +
                                                    $"ORDER BY T0.\"DocEntry\" ASC, T1.\"LineNum\" ASC";

                                using (var cmdSplit = new HanaCommand(splitQuery, connSplit))
                                using (var rdrSplit = cmdSplit.ExecuteReader())
                                {
                                    while (rdrSplit.Read() && remainingQty > 0)
                                    {
                                        int nextDocEntry = Convert.ToInt32(rdrSplit["DocEntry"]);
                                        int nextLineNum = Convert.ToInt32(rdrSplit["LineNum"]);
                                        double nextOpenQty = Convert.ToDouble(rdrSplit["OpenQty"]);

                                        double takeQty = Math.Min(remainingQty, nextOpenQty);

                                        oGRPO.Lines.BaseType = 22;
                                        oGRPO.Lines.BaseEntry = nextDocEntry;
                                        oGRPO.Lines.BaseLine = nextLineNum;
                                        oGRPO.Lines.InventoryQuantity = takeQty;
                                        oGRPO.Lines.WarehouseCode = cmbTangkiNo.Text;

                                        oGRPO.Lines.BatchNumbers.BatchNumber = tbTiketNum.Text;
                                        oGRPO.Lines.BatchNumbers.Quantity = takeQty;
                                        oGRPO.Lines.BatchNumbers.ManufacturingDate = DateTime.Today;
                                        oGRPO.Lines.BatchNumbers.Add();

                                        oGRPO.Lines.Add();

                                        remainingQty -= takeQty;
                                    }
                                }
                                connSplit.Close();
                            }
                        }
                        catch (Exception exSplit)
                        {
                            System.Diagnostics.Debug.WriteLine("Split Query Error: " + exSplit.Message);
                        }

                        if (remainingQty > 0)
                        {
                            MessageBox.Show($"Perhatian: Total kuota seluruh PO aktif dalam kontrak '{contractNo}' masih kurang {remainingQty:N2} MT untuk menampung seluruh muatan truk ini.",
                                "Peringatan Kuota Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                int retval = 0;

                retval = oGRPO.Add();

                if (retval == 0)
                {
                    if (oSBOConnection.oCompany.InTransaction)
                    {
                        oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_Commit);
                    }
                    string wbStatus = "SYNCSAP";
                    string wbWeighout = "Y";
                    UpdateWBUDO(tbTiketNum.Text, wbStatus, wbWeighout);
                }
                else
                {
                    if (oSBOConnection.oCompany.InTransaction)
                    {
                        oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                    }
                    oSBOConnection.oCompany.Disconnect();
                    MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (Exception ex)
            {
                if (oSBOConnection.oCompany != null && oSBOConnection.oCompany.InTransaction)
                {
                    oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                }
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SyncDO(string itemCode, string whsCode)
        {
            int docEntrySO = Convert.ToInt32(lbValDocEntSAP.Text);
            int baseLineSO = Convert.ToInt32(lblValLineNumSAP.Text);
            string docType = cbDocType.Text;
            string isIns = "";
            int baseType = 0;
            int docEntryReserve = 0;
            int baseLineReserve = 0;

            try
            {
                string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

                var connection = new HanaConnection(connectionString);

                using (connection)
                {
                    connection.Open();

                    string queryString = $"CALL SOL_SP_ADDON_WB_SEARCH_PO_SO_RESERVE('{docType}', {docEntrySO})";

                    using (var command = new HanaCommand(queryString, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                isIns = reader["ISINS"].ToString();
                                baseType = Convert.ToInt32(reader["BASETYPE"]);
                                docEntryReserve = Convert.ToInt32(reader["DOCENTRY"]);
                                baseLineReserve = Convert.ToInt32(reader["LINENUM"]);
                            }
                        }
                    }
                }
                connection.Close();

                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                oSBOConnection.oCompany.StartTransaction();

                Documents oDO = null;
                oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

                oDO.DocDate = DateTime.Today;
                oDO.UserFields.Fields.Item("U_SOL_KODE_DOKUMEN").Value = cmbKodeDokPabean.Text?.Length >= 7 ? cmbKodeDokPabean.Text.ToString().Substring(0, 7) : string.Empty;
                oDO.UserFields.Fields.Item("U_SOL_NOMOR_DOKUMEN").Value = tbNoDokPabean.Text;
                oDO.UserFields.Fields.Item("U_SOL_TANGGAL_DOKUMEN").Value = dtTglDokPabean.Value;
                oDO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;
                oDO.UserFields.Fields.Item("U_SOL_NO_SPB").Value = tbNomorSPB.Text;
                oDO.UserFields.Fields.Item("U_SOL_NOMOR_AJU").Value = tbNomorAju.Text;
                oDO.UserFields.Fields.Item("U_SOL_NAMA_SUPIR").Value = tbNamaSupir.Text;
                oDO.UserFields.Fields.Item("U_SOL_JENIS_TRUCK").Value = tbJnsTruck.Text;
                oDO.UserFields.Fields.Item("U_SOL_NO_PLAT").Value = tbNopol.Text;
                //oDO.Comments = "Generated by Weighbridge Addon.";


                if (!String.IsNullOrEmpty(tbTransporterInt.Text))
                {
                    oDO.UserFields.Fields.Item("U_SOL_VENDOR").Value = tbTransporterInt.Text;
                }
                else if (!String.IsNullOrEmpty(tbTransporterEks.Text))
                {
                    oDO.UserFields.Fields.Item("U_SOL_NAMA_TRANSPORTER").Value = tbTransporterEks.Text;
                }

                if (isIns == "Y" && baseType == 17)
                {
                    oDO.Lines.BaseType = 13;
                    oDO.Lines.BaseEntry = docEntryReserve;
                    oDO.Lines.BaseLine = baseLineReserve;
                    //oDO.Lines.ItemCode = grpodetail.ItemCode;
                    //oDO.Lines.ItemDescription = grpodetail.ItemName;
                    //oDO.Lines.Quantity = Convert.ToDouble(tbQtyNetto.Text);
                    oDO.Lines.InventoryQuantity = Convert.ToDouble(tbQtyNetto.Text);
                    oDO.Lines.WarehouseCode = cmbTangkiNo.Text;

                    oDO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = tbBrutoKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = tbTaraKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = tbNettoKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = tbFFAKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = tbMOISTKebun.Text;

                    Recordset rs = (Recordset)oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                    rs.DoQuery($"CALL SOL_SP_ADDON_WB_GET_BATCHNUM_QTY('{itemCode}', '{whsCode}')");

                    double remainingQty = Convert.ToDouble(tbQtyNetto.Text);

                    while (!rs.EoF && remainingQty > 0)
                    {
                        string batchNum = rs.Fields.Item("BATCHNUM").Value.ToString();
                        double batchQty = Convert.ToDouble(rs.Fields.Item("QTY").Value);

                        double takeQty;

                        if (batchQty >= remainingQty)
                        {
                            takeQty = remainingQty; // ambil sisanya saja
                        }
                        else
                        {
                            takeQty = batchQty; // habiskan batch ini
                        }

                        oDO.Lines.BatchNumbers.BatchNumber = batchNum;
                        oDO.Lines.BatchNumbers.Quantity = takeQty;
                        oDO.Lines.BatchNumbers.Add();

                        remainingQty -= takeQty;

                        rs.MoveNext();
                    }

                    oDO.Lines.Add();
                }
                else
                {
                    oDO.Lines.BaseType = 17;
                    oDO.Lines.BaseEntry = Convert.ToInt32(lbValDocEntSAP.Text);
                    oDO.Lines.BaseLine = Convert.ToInt32(lblValLineNumSAP.Text);
                    //oDO.Lines.ItemCode = grpodetail.ItemCode;
                    //oDO.Lines.ItemDescription = grpodetail.ItemName;
                    //oDO.Lines.Quantity = Convert.ToDouble(tbQtyNetto.Text);
                    oDO.Lines.InventoryQuantity = Convert.ToDouble(tbQtyNetto.Text);
                    oDO.Lines.WarehouseCode = cmbTangkiNo.Text;

                    oDO.Lines.UserFields.Fields.Item("U_SOL_BRUTO_KEBUN").Value = tbBrutoKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_TARA_KEBUN").Value = tbTaraKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_NETTO_KEBUN").Value = tbNettoKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_FFA_KEBUN").Value = tbFFAKebun.Text;
                    oDO.Lines.UserFields.Fields.Item("U_SOL_MOIST_KEBUN").Value = tbMOISTKebun.Text;

                    Recordset rs = (Recordset)oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                    rs.DoQuery($"CALL SOL_SP_ADDON_WB_GET_BATCHNUM_QTY('{itemCode}', '{whsCode}')");

                    double remainingQty = Convert.ToDouble(tbQtyNetto.Text);

                    while (!rs.EoF && remainingQty > 0)
                    {
                        string batchNum = rs.Fields.Item("BATCHNUM").Value.ToString();
                        double batchQty = Convert.ToDouble(rs.Fields.Item("QTY").Value);

                        double takeQty;

                        if (batchQty >= remainingQty)
                        {
                            takeQty = remainingQty; // ambil sisanya saja
                        }
                        else
                        {
                            takeQty = batchQty; // habiskan batch ini
                        }

                        oDO.Lines.BatchNumbers.BatchNumber = batchNum;
                        oDO.Lines.BatchNumbers.Quantity = takeQty;
                        oDO.Lines.BatchNumbers.Add();

                        remainingQty -= takeQty;

                        rs.MoveNext();
                    }

                    oDO.Lines.Add();
                }

                int retval = 0;

                retval = oDO.Add();

                if (retval == 0)
                {
                    if (oSBOConnection.oCompany.InTransaction)
                    {
                        oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_Commit);
                    }
                    string wbStatus = "SYNCSAP";
                    string wbWeighout = "Y";
                    UpdateWBUDO(tbTiketNum.Text, wbStatus, wbWeighout);
                }
                else
                {
                    if (oSBOConnection.oCompany.InTransaction)
                    {
                        oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                    }
                    oSBOConnection.oCompany.Disconnect();
                    MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (Exception ex)
            {
                if (oSBOConnection.oCompany != null && oSBOConnection.oCompany.InTransaction)
                {
                    oSBOConnection.oCompany.EndTransaction(BoWfTransOpt.wf_RollBack);
                }
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void UpdateWBUDO(string tiketNum, string wbStatus, string wbWeighout)
        {
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);
            var connection = new HanaConnection(connectionString);

            string code = "";

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + tiketNum + "')";

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
                                code = reader["CODE"].ToString();
                            }
                        }
                    }
                }

                connection.Close();
            }

            try
            {
                string kodeDokPabean = "";
                string tangkiNo = "";

                if (cmbKodeDokPabean.SelectedIndex != -1)
                {
                    kodeDokPabean = cmbKodeDokPabean.Text.ToString();
                }

                if (cmbTangkiNo.SelectedIndex != -1)
                {
                    tangkiNo = cmbTangkiNo.Text.ToString();
                }

                //Declare all SAPbobsCOM untuk DI API UDO
                SAPbobsCOM.GeneralService oGeneralService;
                SAPbobsCOM.GeneralData oGeneralData;
                SAPbobsCOM.GeneralDataParams oGeneralParams;

                SAPbobsCOM.CompanyService oSTR = null;
                oSTR = oSBOConnection.oCompany.GetCompanyService();

                //Get a handle to the Stock Request UDO
                oGeneralService = oSTR.GetGeneralService("OWB");
                oGeneralParams = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);

                oGeneralParams.SetProperty("Code", code);
                oGeneralData = oGeneralService.GetByParams(oGeneralParams);
                
                //Specify data for main UDO (Header)
                oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
                oGeneralData.SetProperty("Code", code); oGeneralData.SetProperty("U_SOL_DOC_TYPE", cbDocType.SelectedItem.ToString());
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
                oGeneralData.SetProperty("U_SOL_BERAT_KELUAR", Convert.ToDouble(tbQtyKeluar.Text));
                oGeneralData.SetProperty("U_SOL_BERAT_NETTO", Convert.ToDouble(tbQtyNetto.Text));
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
                oGeneralData.SetProperty("U_SOL_TANGGAL_MASUK", Convert.ToDateTime(valTanggalMasuk.Text));
                oGeneralData.SetProperty("U_SOL_WAKTU_MASUK", Convert.ToDateTime(valWaktuMasuk.Text));
                oGeneralData.SetProperty("U_SOL_TANGGAL_KELUAR", Convert.ToDateTime(DateTime.Today));
                oGeneralData.SetProperty("U_SOL_WAKTU_KELUAR", DateTime.Now);
                oGeneralData.SetProperty("U_SOL_SEAL_NO", tbSealNo.Text);
                oGeneralData.SetProperty("U_SOL_DO_NO", tbDONo.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_1_NO", tbContNo1.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_2_NO", tbContNo2.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_TANGKI", tangkiNo);
                oGeneralData.SetProperty("U_SOL_NOTE", tbNote.Text);
                oGeneralData.SetProperty("U_SOL_USER", lblUsername.Text);
                oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
                oGeneralData.SetProperty("U_SOL_WEIGHOUT", wbWeighout);
                oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumSAP.Text));

                if (!String.IsNullOrEmpty(tbTransporterInt.Text))
                {
                    oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", tbTransporterInt.Text);
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
                oGeneralService.Update(oGeneralData);
                oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

                oSBOConnection.oCompany.Disconnect();

                //MessageBox.Show("Data updated. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                MessageBox.Show("Data Synced to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
