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
    public partial class timbangMasukUpdateGRPODOForm : Form
    {
        HANAConnetion hanaConn = new HANAConnetion();
        private SerialPort _serialPort;
        private WeighStatusForm _statusForm;

        public string timbangType; //A = Auto, M = Manual
        private string noKontrak = "";

        SBOConnection oSBOConnection = new SBOConnection();

        public timbangMasukUpdateGRPODOForm()
        {
            InitializeComponent();
        }

        private void UpdateDeliveryOrderForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataComboBox();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;

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

        private void runningTime_Tick(object sender, EventArgs e)
        {
            lbDateTime.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
        }

        private void ShowReadingDialog(string type)
        {
            _statusForm = new WeighStatusForm();
            _statusForm.ShowReading();
            _statusForm.Show(this);             // non-blocking, keeps serial reading active
        }

        #region Update DO

        private void chbManualMasuk_CheckedChanged(object sender, EventArgs e)
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

        private void tbQtyTimbangMasuk_KeyPress(object sender, KeyPressEventArgs e)
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

        private void btnTimbangMasuk_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();

                _serialPort.DiscardInBuffer();

                // Show reading dialog
                ShowReadingDialog("Timbang Masuk");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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
                    string docNum = tbBaseDoc.Text;

                    if (String.IsNullOrEmpty(docType) || String.IsNullOrEmpty(docNum))
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

                            string queryString2 = "CALL SOL_SP_ADDON_WB_SEARCH_BASE_DOC_GRPO_DO('" + docType + "','" + docNum + "')";

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
                                            MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                            tbTiketNum.Text = GenerateTiketNumber();
                                            tbTiketNum.Enabled = false;

                                            if (reader2["TANGGALDOKUMEN"] == DBNull.Value)
                                            {
                                                dtTglDokPabean.CustomFormat = " ";
                                            }
                                            else
                                            {
                                                dtTglDokPabean.Value = Convert.ToDateTime(reader2["TANGGALDOKUMEN"]);
                                            }

                                            //objType = Convert.ToInt32(reader2["OBJTYPE"]);
                                            docEntry = Convert.ToInt32(reader2["DOCENTRY"]);
                                            noKontrak = reader2["KONTRAK"] != DBNull.Value ? reader2["KONTRAK"].ToString() : "";
                                            string itemCode = reader2["ITEMCODE"].ToString();
                                            int linenumSAP = Convert.ToInt32(reader2["LINENUM"]);

                                            //lblValDocNo.Text = ": " + docNum;
                                            //lblValKontrak.Text = ": " + noKontrak;
                                            tbCommodity.Text = itemCode;
                                            tbKodeDokPabean.Text = reader2["KODEDOKUMEN"].ToString();
                                            tbNoDokPabean.Text = reader2["NOMORDOKUMEN"].ToString();
                                            tbNomorSPB.Text = reader2["NOMORSPB"].ToString();
                                            tbNomorAju.Text = reader2["NOMORAJU"].ToString();
                                            lblValDocEntDOSAP.Text = docEntry.ToString();
                                            lblValLineNumDOSAP.Text = linenumSAP.ToString();

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
            if (string.IsNullOrWhiteSpace(tbKodeDokPabean.Text) ||
                string.IsNullOrWhiteSpace(tbNoDokPabean.Text) ||
                dtTglDokPabean.CustomFormat == " ")
            {
                MessageBox.Show("Dokumen Pabean (Kode, Nomor, dan Tanggal) wajib diisi sebelum menyimpan data timbang!",
                    "Validasi Pabean - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void btnWBSave_Click(object sender, EventArgs e)
        {
            if (!ValidasiDokumenPabean())
            {
                return;
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
                oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", tbBaseDoc.Text);
                oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lblValDocEntDOSAP.Text));
                oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
                oGeneralData.SetProperty("U_SOL_COMMODITY", tbCommodity.Text);
                oGeneralData.SetProperty("U_SOL_NO_KONTRAK", noKontrak);
                oGeneralData.SetProperty("U_SOL_NOPOL", tbNopol.Text);
                oGeneralData.SetProperty("U_SOL_NAMA_SUPIR", tbNamaSupir.Text);
                oGeneralData.SetProperty("U_SOL_NO_SIM_SUPIR", tbSIMSupir.Text);
                oGeneralData.SetProperty("U_SOL_JENIS_TRUCK", tbJnsTruck.Text);
                oGeneralData.SetProperty("U_SOL_BERAT_MASUK", Convert.ToDouble(tbQtyMasuk.Text));
                oGeneralData.SetProperty("U_SOL_KODE_DOKUMEN", tbKodeDokPabean.Text);
                
                oGeneralData.SetProperty("U_SOL_NO_DOKUMEN", tbNoDokPabean.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_SPB", tbNomorSPB.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_AJU", tbNomorAju.Text);
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
                oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumDOSAP.Text));

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

                if (dtTglDokPabean.CustomFormat != " ")
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

        private void UpdateDO(int docEntryDOSAP, int lineNumDOSAP, double qtyTimbangDO)
        {

            try
            {
                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                SAPbobsCOM.Documents oDO = null;
                oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

                oDO.GetByKey(docEntryDOSAP);

                oDO.Lines.SetCurrentLine(Convert.ToInt32(lineNumDOSAP));
                oDO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyTimbangDO;

                int retval = 0;

                retval = oDO.Update();

                if (retval == 0)
                {
                    oSBOConnection.oCompany.Disconnect();
                    MessageBox.Show("Data Updated to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    ClearAllField();
                }
                else
                {
                    oSBOConnection.oCompany.Disconnect();
                    MessageBox.Show("Error : " + oSBOConnection.oCompany.GetLastErrorDescription().Replace("'", "").Replace("\"", ""), "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    ClearAllField();
                }

            }
            catch (Exception ex)
            {
                oSBOConnection.oCompany.Disconnect();
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                ClearAllField();
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
            cbTMManual.Enabled = true;
            tbSealNo.Enabled = true;
            tbDONo.Enabled = true;
            tbNote.Enabled = true;
            //tbKodeDokPabean.Enabled = true;
            //tbNoDokPabean.Enabled = true;
            //dtTglDokPabean.Enabled = true;
            //tbNomorSPB.Enabled = true;
            //tbNomorAju.Enabled = true;
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
            cbTMManual.Enabled = false;
            btnTimbangMasuk.Enabled = false;
            //btnSubmitUjiLab.Enabled = false;
            btnWBSave.Enabled = false;
            tbNoDokPabean.Enabled = false;
            //tbKodeDokPabean.Enabled = false;
            //dtTglDokPabean.Enabled = false;
            //tbNomorSPB.Enabled = false;
            //tbNomorAju.Enabled = false;
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
            //lbValDocEntSAP.Text = String.Empty;
            //lblValDocNo.Text = String.Empty;
            //lblValKontrak.Text = String.Empty;
            cbDocType.Text = String.Empty;
            tbBaseDoc.Text = String.Empty;
            tbTiketNum.Text = String.Empty;
            tbNopol.Text = String.Empty;
            tbNamaSupir.Text = String.Empty;
            tbSIMSupir.Text = String.Empty;
            tbJnsTruck.Text = String.Empty;
            tbQtyMasuk.Text = "0";
            tbNoDokPabean.Text = String.Empty;
            tbKodeDokPabean.Text = String.Empty;
            tbNomorSPB.Text = String.Empty;
            tbNomorAju.Text = String.Empty;
            tbContNo1.Text = String.Empty;
            tbContNo2.Text = String.Empty;
            tbSealNo.Text = String.Empty;
            tbDONo.Text = String.Empty;
            tbNote.Text = String.Empty;
            tbCommodity.Text = String.Empty;
            dtTglDokPabean.Value = DateTime.Now;
            cbTMManual.Checked = false;
            cbTransInt.Text = String.Empty;
            tbTransporterEks.Text = String.Empty;
            noKontrak = "";
        }


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
    }
}
