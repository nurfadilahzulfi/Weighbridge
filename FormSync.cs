using Sap.Data.Hana;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;
using Weighbridge.Core;
using Weighbridge.Core.Config;
using Weighbridge.Core.Encryptions;
using Weighbridge.XML;

namespace Weighbridge
{
    public partial class FormSync : Form
    {
        private WBConfig _wbConfig;
        private SBOConfig _sboConfig;
        private WBConnection wbConn;

        private Encryption encryption;

        private SerialPort _serialPort;
        private bool _isWeighIn = false;
        private bool _isWeighOut = false;
        private WeighStatusForm _statusForm;
        private StringBuilder _dataBuffer = new StringBuilder();

        public string timbangType; //A = Auto, M = Manual

        ModGlobal modGlobal = new ModGlobal();
        SBOConnection oSBOConnection = new SBOConnection();
        HANAConnetion hanaConn = new HANAConnetion();

        public FormSync()
        {
            InitializeComponent();
            //this.AcceptButton = btnLogin;
        }

        private void FormSync_Load(object sender, EventArgs e)
        {
            //LoadConfig();
            //InitializeSerialPort();
            //LoadDataKodeOokumenPabean();
            //ShowLoginOnly();
            //runningTime.Start();
            ApplyMenuPermissions();
            //lblUsername.Text = SessionUser.username;
        }

        private void ApplyMenuPermissions()
        {
            switch (SessionUser.userRole)  // ← read from Session directly
            {
                case "SU":
                    konfigurasiToolStripMenuItem.Visible = true;
                    weighbridgeToolStripMenuItem.Visible = true;
                    updateGRPODOToolStripMenuItem.Visible = true;
                    break;

                case "ADMIN":
                    konfigurasiToolStripMenuItem.Visible = false;
                    weighbridgeToolStripMenuItem.Visible = true;
                    updateGRPODOToolStripMenuItem.Visible = true;
                    break;
            }
        }

        //private void InitializeSerialPort()
        //{
        //    modGlobal = global();

        //    //wbConn = new WBConnection(modGlobal);

        //    _serialPort = new SerialPort()
        //    {
        //        PortName = modGlobal.portName,        // Change to your COM port
        //        BaudRate = Convert.ToInt32(modGlobal.baudRate),          // Common: 9600, 19200, 38400
        //        DataBits = Convert.ToInt32(modGlobal.dataBits),
        //        Parity = Parity.Even,
        //        StopBits = StopBits.One,
        //        Handshake = Handshake.None,
        //        ReadTimeout = Convert.ToInt32(modGlobal.readTimeout),       // Increase timeout to 3 seconds
        //        WriteTimeout = Convert.ToInt32(modGlobal.writeTimeout),
        //        NewLine = "\r\n"
        //    };

        //    _serialPort.DataReceived += SerialPort_DataReceived;
        //}

        //private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    try
        //    {
        //        string rawData = _serialPort.ReadExisting(); // or ReadExisting()
        //        string weight = ParseWeight(rawData);

        //        // Update UI on main thread
        //        this.Invoke((MethodInvoker)delegate
        //        {
        //            if (_isWeighIn)
        //            {
        //                tbQtyMasuk.Text = weight;
        //                //tbQtyTestTimbang.Text = weight;
        //                _isWeighIn = false;

        //                // Update dialog → show success → auto close
        //                _statusForm?.ShowSuccess(weight);
        //            }
        //            else if (_isWeighOut)
        //            {
        //                tbQtyKeluar.Text = weight;
        //                //tbQtyTestTimbang.Text = weight;
        //                _isWeighOut = false;

        //                // Update dialog → show success → auto close
        //                _statusForm?.ShowSuccess(weight);
        //            }

        //            tbQtyTestTimbang.Text = weight;
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        this.Invoke((MethodInvoker)delegate
        //        {
        //            tbQtyTestTimbang.Text = "Error " + ex.Message;

        //            _statusForm?.Close();
        //            MessageBox.Show($"Read error: {ex.Message}");
        //        });
        //    }
        //}

        // Parse weight from raw data string
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

        private void ShowReadingDialog(string type)
        {
            _statusForm = new WeighStatusForm();
            _statusForm.ShowReading();
            _statusForm.Show(this);             // non-blocking, keeps serial reading active
        }

        private void runningTime_Tick(object sender, EventArgs e)
        {
            //lbDateTime.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
            //lbDateTimeDO.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
        }

        #region Weighbridge

        private void cbTMManual_CheckedChanged_1(object sender, EventArgs e)
        {
            //if (cbTMManual.Checked)
            //{
            //    timbangType = "M";
            //    tbQtyMasuk.Enabled = true;
            //    tbQtyMasuk.Text = Convert.ToString(0);
            //    btnTimbangMasuk.Visible = false;
            //}
            //else
            //{
            //    timbangType = "A";
            //    tbQtyMasuk.Enabled = false;
            //    tbQtyMasuk.Text = Convert.ToString(0);
            //    btnTimbangMasuk.Visible = true;
            //}
        }

        private void cbTKManual_CheckedChanged(object sender, EventArgs e)
        {
            //if (cbTKManual.Checked)
            //{
            //    timbangType = "M";
            //    tbQtyKeluar.Enabled = true;
            //    tbQtyKeluar.Text = Convert.ToString(0);
            //    btnTimbangKeluar.Visible = false;
            //}
            //else
            //{
            //    timbangType = "A";
            //    tbQtyKeluar.Enabled = false;
            //    tbQtyKeluar.Text = Convert.ToString(0);
            //    btnTimbangKeluar.Visible = true;
            //}
        }

        private void tbQtyMasuk_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, control characters (like backspace), and the decimal separator
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar !=','))
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


        private void tbQtyKeluar_TextChanged(object sender, EventArgs e)
        {
            //double qtyNetto;

            //if (Double.TryParse(tbQtyMasuk.Text, out double qtyMasuk) && Double.TryParse(tbQtyKeluar.Text, out double qtyKeluar))
            //{

            //    if (qtyMasuk > qtyKeluar)
            //    {
            //        qtyNetto = qtyMasuk - qtyKeluar;

            //        tbQtyNetto.Text = qtyNetto.ToString();
            //    }
            //    else
            //    {
            //        qtyNetto = qtyKeluar - qtyMasuk;

            //        tbQtyNetto.Text = qtyNetto.ToString();
            //    }
            //}
        }

        //private void btnWINTimbangNew_Click(object sender, EventArgs e)
        //{
        //    DialogResult result = MessageBox.Show("Apakah anda yakin untuk Timbang Masuk baru?", "Konfirmasi Timbang Masuk baru", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        //    if (result == DialogResult.Yes)
        //    {
        //        timbangBaruWIN();
        //    }
        //}

        //private void timbangBaruWIN()
        //{
        //    tbTiketNum.Clear();
        //    cbDocType.ResetText();
        //    tbBaseDoc.Clear();
        //    //tbWINTiketSt.Clear();
        //    tbNopol.Clear();
        //    tbNamaSupir.Clear();
        //    tbSIMSupir.Clear();
        //    cbTMManual.Checked = false;
        //    tbQtyMasuk.Text = "0";
        //    gbUjiLab.Visible = false;
        //}

        //private void btnTestWBConn_Click(object sender, EventArgs e)
        //{
        //    modGlobal = global();

        //    wbConn = new WBConnection(modGlobal);

        //    try
        //    {
        //        //wbConn.OnRawDataReceived += raw =>
        //        //{
        //        //    this.Invoke((MethodInvoker)(() =>
        //        //    {
        //        //        tbQtyTestTimbang.Text = raw.Replace("\r", "[CR]").Replace("\n", "[LF]");
        //        //    }));
        //        //};

        //        //wbConn.OnWeightReceived += weight =>
        //        //{
        //        //    this.Invoke((MethodInvoker)(() =>
        //        //    {
        //        //        tbQtyTestTimbang.Text = weight.ToString("F2");
        //        //        //lbTestQtyTimbang.Text = 100.ToString();
        //        //    }));
        //        //};
        //        //wbConn.Connect();

        //        //if (wbConn.IsConnected)
        //        //{
        //        //    lbStatusWBConnection.Text = "Connected";
        //        //    lbStatusWBConnection.ForeColor = Color.Green;
        //        //}
        //        //else
        //        //{
        //        //    lbStatusWBConnection.Text = "Not Connected";
        //        //    lbStatusWBConnection.ForeColor = Color.Red;
        //        //}

        //        //wbConn.Disconnect();
        //        if (!_serialPort.IsOpen)
        //        {
        //            _serialPort.Open();
        //            if (_serialPort.IsOpen)
        //            {
        //                //ShowReadingDialog("Weigh Out");
        //                lbStatusWBConnection.Text = "Connected";
        //                lbStatusWBConnection.ForeColor = Color.Green;
        //            }
        //            else
        //            {
        //                lbStatusWBConnection.Text = "Not Connected";
        //                lbStatusWBConnection.ForeColor = Color.Red;
        //            }
        //            _serialPort.DiscardInBuffer();
        //        }
        //        else
        //        {
        //            _serialPort.Close();
        //            lbStatusWBConnection.Text = "Connection has closed";
        //            lbStatusWBConnection.ForeColor = Color.Blue;
        //            _serialPort.DiscardInBuffer();
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, "Error - Weighbridge Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        private void btnTimbangMasuk_Click(object sender, EventArgs e)
        {
            try
            {
                _isWeighIn = true;
                _isWeighOut = false;

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
                _isWeighIn = false;
                _isWeighOut = true;

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

        //private string GenerateTiketNumber()
        //{
        //    string tiketNum = "";
        //    ModGlobal modGlobal = new ModGlobal();
        //    modGlobal = global();

        //    string wbCategory = modGlobal.prefix.ToString();

        //    HANAConnetion hanaConn = new HANAConnetion();
        //    string connectionString = hanaConn.HanaConnectionString(modGlobal);

        //    var connection = new HanaConnection(connectionString);

        //    using (connection)
        //    {
        //        connection.Open();

        //        string queryString = "CALL SOL_SP_ADDON_WB_GENERATE_TIKET_NUMBER('" + wbCategory + "')";

        //        using (var command = new HanaCommand(queryString, connection))
        //        {
        //            using (var reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    tiketNum = reader["TIKETNUM"].ToString();
        //                }
        //            }
        //        }

        //        connection.Close();
        //    }

        //    return tiketNum;
        //}

        private void btnCariBaseDoc_Click(object sender, EventArgs e)
        {
        //    if (String.IsNullOrEmpty(tbBaseDoc.Text) && String.IsNullOrEmpty(tbTiketNum.Text))
        //    {
        //        MessageBox.Show("Base Document atau Nomor Tiket harus diisi.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    else if(!String.IsNullOrEmpty(tbBaseDoc.Text) && !String.IsNullOrEmpty(tbTiketNum.Text))
        //    {
        //        MessageBox.Show("Hanya bisa mencari dari Base Document atau Nomor Tiket.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    else if (String.IsNullOrEmpty(cbDocType.Text) && !String.IsNullOrEmpty(tbBaseDoc.Text))
        //    {
        //        MessageBox.Show("Pilih Document Type terlebih dahulu", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    else
        //    {
        //        if (!String.IsNullOrEmpty(tbBaseDoc.Text))
        //        {
        //            int objType;
        //            int docEntry;
        //            string docType = cbDocType.SelectedItem.ToString();
        //            string docNum = tbBaseDoc.Text;
        //            string noKontrak;

        //            if (String.IsNullOrEmpty(docType) || String.IsNullOrEmpty(docNum))
        //            {
        //                MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            }
        //            else
        //            {
        //                modGlobal = global();
        //                string connectionString = hanaConn.HanaConnectionString(modGlobal);

        //                var connection = new HanaConnection(connectionString);

        //                using (connection)
        //                {
        //                    connection.Open();

        //                    string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_JOIN_WB_DATA('" + docType + "','" + docNum + "')";

        //                    using (var command = new HanaCommand(queryString, connection))
        //                    {
        //                        using (var reader = command.ExecuteReader())
        //                        {
        //                            if (!reader.HasRows)
        //                            {
        //                                string queryString2 = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC('" + docType + "','" + docNum + "')";

        //                                using (var command2 = new HanaCommand(queryString2, connection))
        //                                {
        //                                    using (var reader2 = command2.ExecuteReader())
        //                                    {
        //                                        if (!reader2.HasRows)
        //                                        {
        //                                            MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //                                            DisableFieldWB();
        //                                            ClearAllField();
        //                                        }
        //                                        else
        //                                        {
        //                                            while (reader2.Read())
        //                                            {
        //                                                MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //                                                tbTiketNum.Text = GenerateTiketNumber();
        //                                                tbTiketNum.Enabled = false;

        //                                                //objType = Convert.ToInt32(reader2["OBJTYPE"]);
        //                                                docEntry = Convert.ToInt32(reader2["DOCENTRY"]);
        //                                                docNum = reader2["DOCNUM"].ToString();
        //                                                noKontrak = reader2["KONTRAK"].ToString();
        //                                                int linenumSAP = Convert.ToInt32(reader2["LINENUM"]);

        //                                                //lblValDocNo.Text = ": " + docNum;
        //                                                //lblValKontrak.Text = ": " + noKontrak;
        //                                                lbValDocEntSAP.Text = docEntry.ToString();
        //                                                lblValLineNumSAP.Text = linenumSAP.ToString();

        //                                                EnableFieldWB();

        //                                                btnCariBaseDoc.Visible = false;
        //                                                btnRefreshData.Visible = true;
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {
        //                                while (reader.Read())
        //                                {
        //                                    MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //                                    tbTiketNum.Text = GenerateTiketNumber();
        //                                    tbTiketNum.Enabled = false;

        //                                    //objType = Convert.ToInt32(reader["OBJTYPE"]);
        //                                    docEntry = Convert.ToInt32(reader["U_SOL_DOC_ENTRY_SAP"]);
        //                                    docNum = reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                    noKontrak = reader["U_SOL_NO_KONTRAK"].ToString();
        //                                    int linenumSAP = Convert.ToInt32(reader["U_SOL_LINENUM_SAP"]);

        //                                    //lblValDocNo.Text = ": " + docNum;
        //                                    //lblValKontrak.Text = ": " + noKontrak;
        //                                    cbDocType.Text = reader["U_SOL_DOC_TYPE"].ToString();
        //                                    tbBaseDoc.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                    tbTiketNum.Text = reader["U_SOL_TIKET_WB"].ToString();
        //                                    //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                    //lblValKontrak.Text = ": " + reader["U_SOL_NO_KONTRAK"].ToString();
        //                                    lblValStatusWB.Text = reader["U_SOL_STATUS"].ToString();
        //                                    tbNopol.Text = reader["U_SOL_NOPOL"].ToString();
        //                                    tbNamaSupir.Text = reader["U_SOL_NAMA_SUPIR"].ToString();
        //                                    tbSIMSupir.Text = reader["U_SOL_NO_SIM_SUPIR"].ToString();
        //                                    tbJnsTruck.Text = reader["U_SOL_JENIS_TRUCK"].ToString();
        //                                    tbBrutoKebun.Text = Convert.ToDecimal(reader["U_SOL_BRUTO_KEBUN"]).ToString("0.##");
        //                                    tbTaraKebun.Text = Convert.ToDecimal(reader["U_SOL_TARA_KEBUN"]).ToString("0.##");
        //                                    tbNettoKebun.Text = Convert.ToDecimal(reader["U_SOL_NETTO_KEBUN"]).ToString("0.##");
        //                                    tbFFAKebun.Text = reader["U_SOL_FFA_KEBUN"].ToString();
        //                                    tbMOISTKebun.Text = reader["U_SOL_MOIST_KEBUN"].ToString();
        //                                    tbFFA.Text = reader["U_SOL_LAB_FFA"].ToString();
        //                                    tbMOIST.Text = reader["U_SOL_LAB_MOIST"].ToString();
        //                                    tbDOBI.Text = reader["U_SOL_LAB_IMP"].ToString();
        //                                    tbIMP.Text = reader["U_SOL_LAB_DOBI"].ToString();
        //                                    tbCAROTINE.Text = reader["U_SOL_LAB_CAROTINE"].ToString();
        //                                    tbCommodity.Text = reader["U_SOL_COMMODITY"].ToString();
        //                                    tbSealNo.Text = reader["U_SOL_SEAL_NO"].ToString();
        //                                    tbDONo.Text = reader["U_SOL_DO_NO"].ToString();
        //                                    tbNote.Text = reader["U_SOL_NOTE"].ToString();
        //                                    cmbKodeDokPabean.SelectedItem = reader["U_SOL_KODE_DOKUMEN"].ToString();
        //                                    dtTglDokPabean.Value = Convert.ToDateTime(reader["U_SOL_TANGGAL_DOKUMEN"]);
        //                                    tbNoDokPabean.Text = reader["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
        //                                    tbNomorSPB.Text = reader["U_SOL_NOMOR_SPB"].ToString();
        //                                    tbNomorAju.Text = reader["U_SOL_NOMOR_AJU"].ToString();
        //                                    tbContNo1.Text = reader["U_SOL_CONTAINER_1_NO"].ToString();
        //                                    tbContNo2.Text = reader["U_SOL_CONTAINER_2_NO"].ToString();
        //                                    tbQtyMasuk.Text = Convert.ToDecimal(reader["U_SOL_BERAT_MASUK"]).ToString("0.##");
        //                                    tbQtyKeluar.Text = Convert.ToDecimal(reader["U_SOL_BERAT_KELUAR"]).ToString("0.##");
        //                                    tbQtyNetto.Text = Convert.ToDecimal(reader["U_SOL_BERAT_NETTO"]).ToString("0.##");
        //                                    lbValDocEntSAP.Text = docEntry.ToString();
        //                                    lblValLineNumSAP.Text = linenumSAP.ToString();
                                            
        //                                    EnableFieldWB();
        //                                    btnCariBaseDoc.Visible = false;
        //                                    btnRefreshData.Visible = true;
        //                                    lblStatusWB.Visible = true;
        //                                    lblValStatusWB.Visible = true;
        //                                }
        //                            }
        //                        }
        //                    }

        //                    connection.Close();
        //                }
        //            }
        //        }
        //        else
        //        {
        //            string noTiket = tbTiketNum.Text;
        //            string noKontrak;

        //            modGlobal = global();
        //            string connectionString = hanaConn.HanaConnectionString(modGlobal);

        //            var connection = new HanaConnection(connectionString);

        //            using (connection)
        //            {
        //                connection.Open();

        //                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_NO_TIKET('" + noTiket + "')";

        //                using (var command = new HanaCommand(queryString, connection))
        //                {
        //                    using (var reader = command.ExecuteReader())
        //                    {
        //                        if (!reader.HasRows)
        //                        {
        //                            MessageBox.Show("Nomor Tiket tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //                            DisableFieldWB();
        //                            ClearAllField();
        //                        }
        //                        else
        //                        {
        //                            while (reader.Read())
        //                            {
        //                                MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //                                tbTiketNum.Enabled = false;

        //                                cbDocType.Text = reader["U_SOL_DOC_TYPE"].ToString();
        //                                tbBaseDoc.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                tbBaseDoc.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
        //                                lbValDocEntSAP.Text = reader["U_SOL_DOC_ENTRY_SAP"].ToString();
        //                                lblValLineNumSAP.Text = reader["U_SOL_LINENUM_SAP"].ToString();
        //                                lblValStatusWB.Text = reader["U_SOL_STATUS"].ToString();
        //                                //lblValKontrak.Text = ": " + reader["U_SOL_NO_KONTRAK"].ToString();
        //                                tbNopol.Text = reader["U_SOL_NOPOL"].ToString();
        //                                tbNamaSupir.Text = reader["U_SOL_NAMA_SUPIR"].ToString();
        //                                tbSIMSupir.Text = reader["U_SOL_NO_SIM_SUPIR"].ToString();
        //                                tbJnsTruck.Text = reader["U_SOL_JENIS_TRUCK"].ToString();
        //                                tbBrutoKebun.Text = Convert.ToDecimal(reader["U_SOL_BRUTO_KEBUN"]).ToString("0.##");
        //                                tbTaraKebun.Text = Convert.ToDecimal(reader["U_SOL_TARA_KEBUN"]).ToString("0.##");
        //                                tbNettoKebun.Text = Convert.ToDecimal(reader["U_SOL_NETTO_KEBUN"]).ToString("0.##");
        //                                tbFFAKebun.Text = reader["U_SOL_FFA_KEBUN"].ToString();
        //                                tbMOISTKebun.Text = reader["U_SOL_MOIST_KEBUN"].ToString();
        //                                tbFFA.Text = reader["U_SOL_LAB_FFA"].ToString();
        //                                tbMOIST.Text = reader["U_SOL_LAB_MOIST"].ToString();
        //                                tbDOBI.Text = reader["U_SOL_LAB_IMP"].ToString();
        //                                tbIMP.Text = reader["U_SOL_LAB_DOBI"].ToString();
        //                                tbCAROTINE.Text = reader["U_SOL_LAB_CAROTINE"].ToString();
        //                                tbCommodity.Text = reader["U_SOL_COMMODITY"].ToString();
        //                                tbSealNo.Text = reader["U_SOL_SEAL_NO"].ToString();
        //                                tbDONo.Text = reader["U_SOL_DO_NO"].ToString();
        //                                tbNote.Text = reader["U_SOL_NOTE"].ToString();
        //                                cmbKodeDokPabean.SelectedItem = reader["U_SOL_KODE_DOKUMEN"].ToString();
        //                                dtTglDokPabean.Value = Convert.ToDateTime(reader["U_SOL_TANGGAL_DOKUMEN"]);
        //                                tbNoDokPabean.Text = reader["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
        //                                tbNomorSPB.Text = reader["U_SOL_NOMOR_SPB"].ToString();
        //                                tbNomorAju.Text = reader["U_SOL_NOMOR_AJU"].ToString();
        //                                tbContNo1.Text = reader["U_SOL_CONTAINER_1_NO"].ToString();
        //                                tbContNo2.Text = reader["U_SOL_CONTAINER_2_NO"].ToString();
        //                                tbQtyMasuk.Text = Convert.ToDecimal(reader["U_SOL_BERAT_MASUK"]).ToString("0.##");
        //                                tbQtyKeluar.Text = Convert.ToDecimal(reader["U_SOL_BERAT_KELUAR"]).ToString("0.##");
        //                                tbQtyNetto.Text = Convert.ToDecimal(reader["U_SOL_BERAT_NETTO"]).ToString("0.##");

        //                                EnableFieldWB();
        //                                btnCariBaseDoc.Visible = false;
        //                                btnRefreshData.Visible = true;
        //                                lblStatusWB.Visible = true;
        //                                lblValStatusWB.Visible = true;
        //                            }
        //                        }
        //                    }
        //                }

        //                connection.Close();
        //            }
        //        }
        //    }
        }

        private void btnWBSave_Click(object sender, EventArgs e)
        {
            //modGlobal = global();
            //string connectionString = hanaConn.HanaConnectionString(modGlobal);
            //var connection = new HanaConnection(connectionString);

            //string tiketNum = tbTiketNum.Text;

            //string code = "";

            //using (connection)
            //{
            //    connection.Open();

            //    string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + tiketNum + "')";

            //    using (var command = new HanaCommand(queryString, connection))
            //    {
            //        using (var reader = command.ExecuteReader())
            //        {
            //            if (!reader.HasRows)
            //            {

            //            }
            //            else
            //            {
            //                while (reader.Read())
            //                {
            //                    code = reader["CODE"].ToString();
            //                }
            //            }
            //        }
            //    }

            //    connection.Close();
            //}

            ////int docEntrySAP = Convert.ToInt32(lbValDocEntSAP.Text);

            //if (String.IsNullOrEmpty(code))
            //{
            //    try
            //    {
            //        string wbStatus = "WEIGHIN";
            //        AddWBUDO(wbStatus);
            //        //UpdatePO(docEntrySAP);

            //        MessageBox.Show("Data added. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //        ClearAllField();
            //        DisableFieldWB();
            //        tbBaseDoc.Focus();
            //        btnCariBaseDoc.Visible = true;
            //        btnRefreshData.Visible = false;
            //    }
            //    catch(Exception ex)
            //    {
            //        MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
            //else
            //{
            //    try
            //    {
            //        if (cbDocType.SelectedItem.ToString() == "PO")
            //        {
            //            SyncGRPO();
            //        }
            //        else
            //        {
            //            SyncDO();
            //        }
            //    }
            //    catch(Exception ex)
            //    {
            //        MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }
            //}
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {

            //DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?", "Confirmation", MessageBoxButtons.YesNo,  MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            //if (result == DialogResult.Yes)
            //{

            //    ClearAllField();
            //    DisableFieldWB();
            //    btnRefreshData.Visible = false;
            //    btnCariBaseDoc.Visible = true;
            //    lblStatusWB.Visible = false;
            //    lblValStatusWB.Visible = false;
            //    tbTiketNum.Enabled = true;
            //}
        }

        private void EnableFieldWB()
        {
            //if (lblValStatusWB.Text == "WEIGHIN")
            //{
            //    tbNopol.Enabled = true;
            //    tbNamaSupir.Enabled = true;
            //    tbSIMSupir.Enabled = true;
            //    tbJnsTruck.Enabled = true;
            //    tbBrutoKebun.Enabled = true;
            //    tbTaraKebun.Enabled = true;
            //    tbNettoKebun.Enabled = true;
            //    tbFFAKebun.Enabled = true;
            //    tbMOISTKebun.Enabled = true;
            //    tbFFA.Enabled = true;
            //    tbMOIST.Enabled = true;
            //    tbIMP.Enabled = true;
            //    tbDOBI.Enabled = true;
            //    tbCAROTINE.Enabled = true;
            //    cbTMManual.Enabled = false;
            //    cbTKManual.Enabled = true;
            //    tbSealNo.Enabled = true;
            //    tbDONo.Enabled = true;
            //    tbNote.Enabled = true;
            //    cmbKodeDokPabean.Enabled = true;
            //    tbNoDokPabean.Enabled = true;
            //    dtTglDokPabean.Enabled = true;
            //    tbNomorSPB.Enabled = true;
            //    tbNomorAju.Enabled = true;
            //    tbContNo1.Enabled = true;
            //    tbContNo2.Enabled = true;
            //    btnTimbangMasuk.Enabled = false;
            //    btnTimbangKeluar.Enabled = true;
            //    //btnSubmitUjiLab.Enabled = true;
            //    btnWBSave.Enabled = true;
            //}
            //else if(lblValStatusWB.Text == "WEIGHOUT" || lblValStatusWB.Text == "SYNCSAP")
            //{
            //    DisableFieldWB();
            //}
            //else
            //{
            //    tbNopol.Enabled = true;
            //    tbNamaSupir.Enabled = true;
            //    tbSIMSupir.Enabled = true;
            //    tbJnsTruck.Enabled = true;
            //    tbBrutoKebun.Enabled = true;
            //    tbTaraKebun.Enabled = true;
            //    tbNettoKebun.Enabled = true;
            //    tbFFAKebun.Enabled = true;
            //    tbMOISTKebun.Enabled = true;
            //    tbFFA.Enabled = true;
            //    tbMOIST.Enabled = true;
            //    tbIMP.Enabled = true;
            //    tbDOBI.Enabled = true;
            //    tbCAROTINE.Enabled = true;
            //    cbTMManual.Enabled = true;
            //    cbTKManual.Enabled = false;
            //    tbSealNo.Enabled = true;
            //    tbDONo.Enabled = true;
            //    tbNote.Enabled = true;
            //    cmbKodeDokPabean.Enabled = true;
            //    tbNoDokPabean.Enabled = true;
            //    dtTglDokPabean.Enabled = true;
            //    tbNomorSPB.Enabled = true;
            //    tbNomorAju.Enabled = true;
            //    tbContNo1.Enabled = true;
            //    tbContNo2.Enabled = true;
            //    btnTimbangMasuk.Enabled = true;
            //    btnTimbangKeluar.Enabled = false;
            //    //btnSubmitUjiLab.Enabled = true;
            //    btnWBSave.Enabled = true;
            //    //tbQtyMasuk.Enabled = true;
            //    //tbQtyKeluar.Enabled = true;
            //    //tbQtyNetto.Enabled = true;
            //}

        }

        //private void DisableFieldWB()
        //{
        //    //cbDocType.Enabled = false;
        //    //tbBaseDoc.Enabled = false;
        //    tbNopol.Enabled = false;
        //    tbNamaSupir.Enabled = false;
        //    tbSIMSupir.Enabled = false;
        //    tbJnsTruck.Enabled = false;
        //    tbBrutoKebun.Enabled = false;
        //    tbTaraKebun.Enabled = false;
        //    tbNettoKebun.Enabled = false;
        //    tbFFAKebun.Enabled = false;
        //    tbMOISTKebun.Enabled = false;
        //    tbFFA.Enabled = false;
        //    tbMOIST.Enabled = false;
        //    tbIMP.Enabled = false;
        //    tbDOBI.Enabled = false;
        //    tbCAROTINE.Enabled = false;
        //    cbTMManual.Enabled = false;
        //    cbTKManual.Enabled = false;
        //    btnTimbangMasuk.Enabled = false;
        //    btnTimbangKeluar.Enabled = false;
        //    //btnSubmitUjiLab.Enabled = false;
        //    btnWBSave.Enabled = false;
        //    tbNoDokPabean.Enabled = false;
        //    cmbKodeDokPabean.Enabled = false;
        //    dtTglDokPabean.Enabled = false;
        //    tbNomorSPB.Enabled = false;
        //    tbNomorAju.Enabled = false;
        //    tbContNo1.Enabled = false;
        //    tbContNo2.Enabled = false;
        //    tbSealNo.Enabled = false;
        //    tbDONo.Enabled = false;
        //    tbNote.Enabled = false;
        //    //tbQtyMasuk.Enabled = false;
        //    //tbQtyKeluar.Enabled = false;
        //    //tbQtyNetto.Enabled = false;
        //}

        //private void ClearAllField()
        //{
        //    lbValDocEntSAP.Text = String.Empty;
        //    //lblValDocNo.Text = String.Empty;
        //    //lblValKontrak.Text = String.Empty;
        //    cbDocType.Text = String.Empty;
        //    tbBaseDoc.Text = String.Empty;
        //    tbTiketNum.Text = String.Empty;
        //    tbNopol.Text = String.Empty;
        //    tbNamaSupir.Text = String.Empty;
        //    tbSIMSupir.Text = String.Empty;
        //    tbJnsTruck.Text = String.Empty;
        //    tbBrutoKebun.Text = String.Empty;
        //    tbTaraKebun.Text = String.Empty;
        //    tbNettoKebun.Text = String.Empty;
        //    tbFFAKebun.Text = String.Empty;
        //    tbMOISTKebun.Text = String.Empty;
        //    tbFFA.Text = String.Empty;
        //    tbMOIST.Text = String.Empty;
        //    tbDOBI.Text = String.Empty;
        //    tbIMP.Text = String.Empty;
        //    tbCAROTINE.Text = String.Empty;
        //    tbQtyMasuk.Text = "0";
        //    tbQtyKeluar.Text = "0";
        //    tbQtyNetto.Text = "0";
        //    lblValStatusWB.Text = String.Empty;
        //    tbNoDokPabean.Text = String.Empty;
        //    cmbKodeDokPabean.Text = String.Empty;
        //    tbNomorSPB.Text = String.Empty;
        //    tbNomorAju.Text = String.Empty;
        //    tbContNo1.Text = String.Empty;
        //    tbContNo2.Text = String.Empty;
        //    tbSealNo.Text = String.Empty;
        //    tbDONo.Text = String.Empty;
        //    tbNote.Text = String.Empty;
        //    tbCommodity.Text = String.Empty;
        //    dtTglDokPabean.Value = DateTime.Now;
        //}

        //private void ClearAllFieldUpdateDO()
        //{
            //cbManualDO.Checked = false;
            //tbDODocNum.Text = String.Empty;
            ////lbValKontrakDO.Text = String.Empty;
            //lblValDocEntDOSAP.Text = String.Empty;
            //lblValLineNumDOSAP.Text = String.Empty;
            //tbQtyTimbangDO.Text = "0";
            //btnUpdateDO.Enabled = false;
            //cbManualDO.Enabled = false;
            //btnTimbangDO.Enabled = false;
        //}

        //private void SyncGRPO()
        //{
            //ModGlobal modGlobal = new ModGlobal();

            //try
            //{
            //    oSBOConnection.connectSBO(global());

            //    oSBOConnection.oCompany.StartTransaction();

            //    Documents oGRPO = null;
            //    oGRPO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

            //    oGRPO.DocDate = DateTime.Today;
            //    oGRPO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;
            //    oGRPO.Comments = "Generated by Weighbridge Addon.";

            //    oGRPO.Lines.BaseType = 22;
            //    oGRPO.Lines.BaseEntry = Convert.ToInt32(lbValDocEntSAP.Text);
            //    oGRPO.Lines.BaseLine = Convert.ToInt32(lblValLineNumSAP.Text);
            //    //oGRPO.Lines.ItemCode = grpodetail.ItemCode;
            //    //oGRPO.Lines.ItemDescription = grpodetail.ItemName;
            //    oGRPO.Lines.Quantity = Convert.ToDouble(tbQtyNetto.Text);
            //    //oGRPO.Lines.WarehouseCode = grpo.Warehouse;

            //    oGRPO.Lines.Add();

            //    int retval = 0;

            //    retval = oGRPO.Add();

            //    if (retval == 0)
            //    {
            //        string wbStatus = "SYNCSAP";
            //        UpdateWBUDO(tbTiketNum.Text, wbStatus);
            //    }
            //    else
            //    {
            //        oSBOConnection.oCompany.Disconnect();
            //        MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }

            //}
            //catch (Exception ex)
            //{
            //    oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        //}

        //private void SyncDO()
        //{
            //ModGlobal modGlobal = new ModGlobal();

            //try
            //{
            //    oSBOConnection.connectSBO(global());

            //    oSBOConnection.oCompany.StartTransaction();

            //    Documents oDO = null;
            //    oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

            //    oDO.DocDate = DateTime.Today;
            //    oDO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;
            //    oDO.Comments = "Generated by Weighbridge Addon.";

            //    oDO.Lines.BaseType = 17;
            //    oDO.Lines.BaseEntry = Convert.ToInt32(lbValDocEntSAP.Text);
            //    oDO.Lines.BaseLine = Convert.ToInt32(lblValLineNumSAP.Text);
            //    //oDO.Lines.ItemCode = grpodetail.ItemCode;
            //    //oDO.Lines.ItemDescription = grpodetail.ItemName;
            //    oDO.Lines.Quantity = Convert.ToDouble(tbQtyNetto.Text);
            //    //oDO.Lines.WarehouseCode = grpo.Warehouse;

            //    oDO.Lines.Add();

            //    int retval = 0;

            //    retval = oDO.Add();

            //    if (retval == 0)
            //    {
            //        string wbStatus = "SYNCSAP";
            //        UpdateWBUDO(tbTiketNum.Text, wbStatus);
            //    }
            //    else
            //    {
            //        oSBOConnection.oCompany.Disconnect();
            //        MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    }

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        //}

        //private void AddWBUDO(string wbStatus)
        //{
            //modGlobal = global();
            //string connectionString = hanaConn.HanaConnectionString(modGlobal);
            //var connection = new HanaConnection(connectionString);

            //string code = "";

            //using (connection)
            //{
            //    connection.Open();

            //    string queryString = "CALL SOL_SP_ADDON_WB_MAX_UDO_CODE()";

            //    using (var command = new HanaCommand(queryString, connection))
            //    {
            //        using (var reader = command.ExecuteReader())
            //        {
            //            if (!reader.HasRows)
            //            {
                            
            //            }
            //            else
            //            {
            //                while (reader.Read())
            //                {
            //                    code = "WB" + reader["CODE"].ToString();
            //                }
            //            }
            //        }
            //    }

            //    connection.Close();
            //}

            //try
            //{
            //    oSBOConnection.connectSBO(global());

            //    //Declare all SAPbobsCOM untuk DI API UDO
            //    SAPbobsCOM.GeneralService oGeneralService;
            //    SAPbobsCOM.GeneralData oGeneralData;

            //    SAPbobsCOM.CompanyService oSTR = null;
            //    oSTR = oSBOConnection.oCompany.GetCompanyService();

            //    //Get a handle to the Stock Request UDO
            //    oGeneralService = oSTR.GetGeneralService("OWB");

            //    //Specify data for main UDO (Header)
            //    oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
            //    oGeneralData.SetProperty("Code", code);
            //    oGeneralData.SetProperty("U_SOL_DOC_TYPE", cbDocType.SelectedItem.ToString());
            //    oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", tbBaseDoc.Text);
            //    oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lbValDocEntSAP.Text));
            //    oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
            //    //oGeneralData.SetProperty("U_SOL_NO_KONTRAK", lblValKontrak.Text);
            //    oGeneralData.SetProperty("U_SOL_NOPOL", tbNopol.Text);
            //    oGeneralData.SetProperty("U_SOL_NAMA_SUPIR", tbNamaSupir.Text);
            //    oGeneralData.SetProperty("U_SOL_NO_SIM_SUPIR", tbSIMSupir.Text);
            //    oGeneralData.SetProperty("U_SOL_JENIS_TRUCK", tbJnsTruck.Text);
            //    oGeneralData.SetProperty("U_SOL_BERAT_MASUK", Convert.ToDouble(tbQtyMasuk.Text));
            //    oGeneralData.SetProperty("U_SOL_BERAT_KELUAR", Convert.ToDouble(tbQtyKeluar.Text));
            //    oGeneralData.SetProperty("U_SOL_BERAT_NETTO", Convert.ToDouble(tbQtyNetto.Text));
            //    oGeneralData.SetProperty("U_SOL_BRUTO_KEBUN", Convert.ToDouble(tbBrutoKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_TARA_KEBUN", Convert.ToDouble(tbTaraKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_NETTO_KEBUN", Convert.ToDouble(tbNettoKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_FFA_KEBUN", tbFFAKebun.Text);
            //    oGeneralData.SetProperty("U_SOL_MOIST_KEBUN", tbMOIST.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_FFA", tbFFA.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_MOIST", tbMOIST.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_IMP", tbIMP.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_DOBI", tbDOBI.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_CAROTINE", tbCAROTINE.Text);
            //    oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
            //    oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumSAP.Text));

            //    //Add records
            //    oGeneralService.Add(oGeneralData);

            //    oSBOConnection.oCompany.Disconnect();
            //}
            //catch (Exception ex)
            //{
            //    oSBOConnection.oCompany.Disconnect();
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        //}

        //private void UpdateWBUDO(string tiketNum, string wbStatus)
        //{
            //modGlobal = global();
            //string connectionString = hanaConn.HanaConnectionString(modGlobal);
            //var connection = new HanaConnection(connectionString);

            //string code = "";

            //using (connection)
            //{
            //    connection.Open();

            //    string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + tiketNum + "')";

            //    using (var command = new HanaCommand(queryString, connection))
            //    {
            //        using (var reader = command.ExecuteReader())
            //        {
            //            if (!reader.HasRows)
            //            {

            //            }
            //            else
            //            {
            //                while (reader.Read())
            //                {
            //                    code = reader["CODE"].ToString();
            //                }
            //            }
            //        }
            //    }

            //    connection.Close();
            //}

            //try
            //{
            //    //oSBOConnection.connectSBO(global());

            //    //Declare all SAPbobsCOM untuk DI API UDO
            //    SAPbobsCOM.GeneralService oGeneralService;
            //    SAPbobsCOM.GeneralData oGeneralData;
            //    SAPbobsCOM.GeneralDataParams oGeneralParams;

            //    SAPbobsCOM.CompanyService oSTR = null;
            //    oSTR = oSBOConnection.oCompany.GetCompanyService();

            //    //Get a handle to the Stock Request UDO
            //    oGeneralService = oSTR.GetGeneralService("OWB");
            //    oGeneralParams = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);

            //    oGeneralParams.SetProperty("Code", code);
            //    oGeneralData = oGeneralService.GetByParams(oGeneralParams);

            //    //Specify data for main UDO (Header)
            //    oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
            //    oGeneralData.SetProperty("Code", code);
            //    oGeneralData.SetProperty("U_SOL_DOC_TYPE", cbDocType.SelectedItem.ToString());
            //    oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lbValDocEntSAP.Text));
            //    oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", tbBaseDoc.Text);
            //    oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
            //    oGeneralData.SetProperty("U_SOL_NOPOL", tbNopol.Text);
            //    oGeneralData.SetProperty("U_SOL_NAMA_SUPIR", tbNamaSupir.Text);
            //    oGeneralData.SetProperty("U_SOL_NO_SIM_SUPIR", tbSIMSupir.Text);
            //    oGeneralData.SetProperty("U_SOL_JENIS_TRUCK", tbJnsTruck.Text);
            //    oGeneralData.SetProperty("U_SOL_BERAT_MASUK", Convert.ToDouble(tbQtyMasuk.Text));
            //    oGeneralData.SetProperty("U_SOL_BERAT_KELUAR", Convert.ToDouble(tbQtyKeluar.Text));
            //    oGeneralData.SetProperty("U_SOL_BERAT_NETTO", Convert.ToDouble(tbQtyNetto.Text));
            //    oGeneralData.SetProperty("U_SOL_BRUTO_KEBUN", Convert.ToDouble(tbBrutoKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_TARA_KEBUN", Convert.ToDouble(tbTaraKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_NETTO_KEBUN", Convert.ToDouble(tbNettoKebun.Text));
            //    oGeneralData.SetProperty("U_SOL_FFA_KEBUN", tbFFAKebun.Text);
            //    oGeneralData.SetProperty("U_SOL_MOIST_KEBUN", tbMOIST.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_FFA", tbFFA.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_MOIST", tbMOIST.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_IMP", tbIMP.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_DOBI", tbDOBI.Text);
            //    oGeneralData.SetProperty("U_SOL_LAB_CAROTINE", tbCAROTINE.Text);
            //    oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
            //    oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumSAP.Text));

            //    //Add records
            //    oGeneralService.Update(oGeneralData);
            //    oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);

            //    oSBOConnection.oCompany.Disconnect();

            //    //MessageBox.Show("Data updated. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    MessageBox.Show("Data Synced to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //    ClearAllField();
            //    DisableFieldWB();
            //    tbBaseDoc.Focus();
            //    btnCariBaseDoc.Visible = true;
            //    btnRefreshData.Visible = false;

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        //}

        //private void UpdatePO(int docEntrySAP)
        //{
            //modGlobal = global();

            //try
            //{
            //    oSBOConnection.connectSBO(global());

            //    SAPbobsCOM.Documents oPO = null;
            //    oPO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oPurchaseOrders);

            //    oPO.GetByKey(docEntrySAP);

            //    oPO.UserFields.Fields.Item("U_SOL_NO_TIKET_WB").Value = tbTiketNum.Text;

            //    oPO.Update();

            //    oSBOConnection.oCompany.Disconnect();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        //}

        //private void UpdateDO(int docEntryDOSAP, int lineNumDOSAP, double qtyTimbangDO)
        //{
            //modGlobal = global();

            //try
            //{
            //    oSBOConnection.connectSBO(global());

            //    SAPbobsCOM.Documents oDO = null;
            //    oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

            //    oDO.GetByKey(docEntryDOSAP);

            //    oDO.Lines.SetCurrentLine(Convert.ToInt32(lineNumDOSAP));
            //    oDO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyTimbangDO;

            //    int retval = 0;

            //    retval = oDO.Update();

            //    if(retval == 0)
            //    {
            //        oSBOConnection.oCompany.Disconnect();
            //        MessageBox.Show("Data Updated to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            //        ClearAllFieldUpdateDO();
            //    }
            //    else
            //    {
            //        oSBOConnection.oCompany.Disconnect();
            //        MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //        ClearAllFieldUpdateDO();
            //    }

            //}
            //catch (Exception ex)
            //{
            //    oSBOConnection.oCompany.Disconnect();
            //    MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

            //    ClearAllFieldUpdateDO();
            //}
        //}

        #endregion

        #region Update DO

        //private void chbManualDO_CheckedChanged(object sender, EventArgs e)
        //{
        //    if (cbManualDO.Checked)
        //    {
        //        timbangType = "M";
        //        tbQtyTimbangDO.Enabled = true;
        //        tbQtyTimbangDO.Text = Convert.ToString(0);
        //        btnTimbangDO.Visible = false;
        //    }
        //    else
        //    {
        //        timbangType = "A"; 
        //        tbQtyTimbangDO.Enabled = false;
        //        tbQtyTimbangDO.Text = Convert.ToString(0);
        //        btnTimbangDO.Visible = true;
        //    }
        //}

        //private void tbQtyTimbangDO_KeyPress(object sender, KeyPressEventArgs e)
        //{
        //    // Allow digits, control characters (like backspace), and the decimal separator
        //    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
        //    {
        //        e.Handled = true; // Prevent the character from being entered
        //    }

        //    // Only allow one decimal point
        //    if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
        //    {
        //        e.Handled = true; // Prevent multiple decimal points
        //    }
        //}

        //private void btnSearchDO_Click(object sender, EventArgs e)
        //{
        //    int objType;
        //    int docEntry;
        //    string docNum = tbDODocNum.Text;
        //    string noKontrak;

        //    if (String.IsNullOrEmpty(docNum))
        //    {
        //        MessageBox.Show("Document Number SAP tidak boleh kosong.", "Error - Weighbridge UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    else
        //    {
        //        ModGlobal modGlobal = new ModGlobal();
        //        modGlobal = global();

        //        HANAConnetion hanaConn = new HANAConnetion();
        //        string connectionString = hanaConn.HanaConnectionString(modGlobal);

        //        var connection = new HanaConnection(connectionString);

        //        using (connection)
        //        {
        //            connection.Open();

        //            string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_DO('" + docNum + "')";

        //            using (var command = new HanaCommand(queryString, connection))
        //            {
        //                using (var reader = command.ExecuteReader())
        //                {
        //                    if (!reader.HasRows)
        //                    {
        //                        MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

        //                        //DisableFieldWB();
        //                        cbManualDO.Enabled = false;
        //                        btnTimbangDO.Enabled = false;
        //                        btnUpdateDO.Enabled = false;
        //                    }
        //                    else
        //                    {
        //                        while (reader.Read())
        //                        {
        //                            MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

        //                            objType = Convert.ToInt32(reader["OBJTYPE"]);
        //                            docEntry = Convert.ToInt32(reader["DOCENTRY"]);
        //                            docNum = reader["DOCNUM"].ToString();
        //                            noKontrak = reader["KONTRAK"].ToString();
        //                            int linenumSAP = Convert.ToInt32(reader["LINENUM"]);

        //                            //lbValKontrakDO.Text = ": " + noKontrak;
        //                            lblValDocEntDOSAP.Text = docEntry.ToString();
        //                            lblValLineNumDOSAP.Text = linenumSAP.ToString();

        //                            cbManualDO.Enabled = true;
        //                            btnTimbangDO.Enabled = true;
        //                            btnUpdateDO.Enabled = true;
        //                            //tbTiketNum.Enabled = false;
        //                        }
        //                    }
        //                }
        //            }

        //            connection.Close();
        //        }
        //    }
        //}

        //private void btnUpdateDO_Click(object sender, EventArgs e)
        //{
        //    int docEntryDOSAP = Convert.ToInt32(lblValDocEntDOSAP.Text);
        //    int lineNumDOSAP = Convert.ToInt32(lblValLineNumDOSAP.Text);
        //    double qtyTimbangDO = Convert.ToDouble(tbQtyTimbangDO.Text);

        //    UpdateDO(docEntryDOSAP, lineNumDOSAP, qtyTimbangDO);
        //}

        #endregion

        #region Configuration

        //private void btnSvConfig_Click(object sender, EventArgs e)
        //{
            
        //}

        
        //private ModGlobal global()
        //{
        //    ModGlobal modGlobal = new ModGlobal();

        //    modGlobal.portName = _wbConfig.portName;
        //    modGlobal.baudRate = _wbConfig.baudRate;
        //    modGlobal.dataBits = _wbConfig.dataBits;
        //    modGlobal.readTimeout = _wbConfig.readTimeout;
        //    modGlobal.writeTimeout = _wbConfig.writeTimeout;
        //    modGlobal.prefix = _wbConfig.prefix;

        //    //modGlobal.serverType = _sboConfig.serverType;
        //    modGlobal.serverName = _sboConfig.serverName;
        //    modGlobal.database = _sboConfig.database;
        //    modGlobal.databaseUsername = _sboConfig.databaseUsername;
        //    modGlobal.databasePassword = encryption.decrypt(_sboConfig.databasePassword);
        //    modGlobal.sboUsername = _sboConfig.sboUsername;
        //    modGlobal.sboPassword = encryption.decrypt(_sboConfig.sboPassword);

        //    return modGlobal;
        //}

        //private void LoadConfig()
        //{
        //    string path = GetConfigPath();
        //    if (!File.Exists(path))
        //    {
        //        ShowConfigTab();
        //    }
        //    else
        //    {
        //        (_wbConfig, _sboConfig) = XMLHandler.LoadConfig();
        //        ModGlobal modGlobal = new ModGlobal();
        //        encryption = new Encryption();

        //        modGlobal = global();

        //        cbPort.Text = modGlobal.portName;
        //        tbBaudRate.Text = modGlobal.baudRate;
        //        tbDataBits.Text = modGlobal.dataBits;
        //        tbReadT.Text = modGlobal.readTimeout.ToString();
        //        tbWriteT.Text = modGlobal.writeTimeout.ToString();
        //        tbPrefixWB.Text = modGlobal.prefix.ToString();

        //        cbSrvType.Text = _sboConfig.serverType;
        //        tbDBSrv.Text = modGlobal.serverName;
        //        tbDBSBO.Text = modGlobal.database;
        //        tbDBUsrnm.Text = modGlobal.databaseUsername;
        //        tbDBPass.Text = modGlobal.databasePassword;
        //        tbSBOUsrnm.Text = modGlobal.sboUsername;
        //        tbSBOPass.Text = modGlobal.sboPassword;

        //        SBOConnection sboConnection = new SBOConnection();

        //        try
        //        {

        //            if (sboConnection.connectionSBO(global()))
        //            {
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message, "Error - SBO Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //            ShowConfigTab();
        //        }
        //    }
        //}

        

        //public static string GetConfigPath()
        //{
        //    string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
        //    Directory.CreateDirectory(folder);

        //    return Path.Combine(folder, "Config.xml");
        //}

        #endregion

        #region Tab Configcuration

        //private void ShowLoginOnly()
        //{
        //    tabCtrlMain.TabPages.Clear();
        //    tabCtrlMain.TabPages.Add(tabLogin);
        //}

        //private void ShowTabAdmin()
        //{
        //    tabCtrlMain.TabPages.Clear();
        //    tabCtrlMain.TabPages.Add(tabWB);
        //    tabCtrlMain.TabPages.Add(tabUpdateDO);
        //    tabCtrlMain.TabPages.Add(tabReport);

        //    tabCtrlMain.SelectedIndex = 0;
        //}

        //private void ShowTabSuperuser()
        //{
        //    tabCtrlMain.TabPages.Clear();
        //    tabCtrlMain.TabPages.Add(tabConfig);
        //    tabCtrlMain.TabPages.Add(tabWB);
        //    tabCtrlMain.TabPages.Add(tabUpdateDO);
        //    tabCtrlMain.TabPages.Add(tabReport);

        //    tabCtrlMain.SelectedIndex = 0;
        //}

        //private void ShowConfigTab()
        //{
        //    foreach (TabPage tab in tabCtrlMain.TabPages.Cast<TabPage>().ToList())
        //    {
        //        if (tab.Name != "tabConfig")
        //            tab.Parent = null;
        //    }
        //    tabCtrlMain.SelectedTab = tabCtrlMain.TabPages["tabConfig"];
        //}

        #endregion

        private void konfigurasiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConfigurationForm configurationForm = new ConfigurationForm();
            configurationForm.Show();
        }

        private void timbangMasukToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WeighbridgeForm weighbridgeForm = new WeighbridgeForm();
            weighbridgeForm.Show();
        }

        private void timbangKeluarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TimbangKeluarForm timbangKeluarForm = new TimbangKeluarForm();
            timbangKeluarForm.Show();
        }

        private void reprintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReprintTiket reprintTiket = new ReprintTiket();
            reprintTiket.Show();
        }

        private void reportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReportForm reportForm = new ReportForm();
            reportForm.Show();
        }

        private void timbangMasukUpdateTS_Click(object sender, EventArgs e)
        {
            timbangMasukUpdateGRPODOForm timbangMasukUpdateGRPODOForm = new timbangMasukUpdateGRPODOForm();
            timbangMasukUpdateGRPODOForm.Show();
        }

        private void timbangKeluarUpdateTS_Click(object sender, EventArgs e)
        {
            timbangKeluarUpdateGRPODOForm timbangKeluarUpdateGRPODOForm = new timbangKeluarUpdateGRPODOForm();
            timbangKeluarUpdateGRPODOForm.Show();
        }
    }
}
