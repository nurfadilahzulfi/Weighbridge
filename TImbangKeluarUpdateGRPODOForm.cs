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
    public partial class timbangKeluarUpdateGRPODOForm : Form
    {
        private SerialPort _serialPort;
        private WeighStatusForm _statusForm;

        HANAConnetion hanaConn = new HANAConnetion();
        SBOConnection oSBOConnection = new SBOConnection();

        public string timbangType; //A = Auto, M = Manual
        private string noKontrak = "";

        public timbangKeluarUpdateGRPODOForm()
        {
            InitializeComponent();
        }

        private void timbangKeluarUpdateGRPODOForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;
        }

        private bool ValidasiDokumenPabean()
        {
            if (string.IsNullOrWhiteSpace(tbKodeDokPabean.Text) ||
                string.IsNullOrWhiteSpace(tbNoDokPabean.Text) ||
                (!dtTglDokPabean.Checked && dtTglDokPabean.CustomFormat == " "))
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
                string tiketNum = tbTiketNum.Text;
                int docEntryDOSAP = Convert.ToInt32(lblValDocEntDOSAP.Text);
                int lineNumDOSAP = Convert.ToInt32(lblValLineNumDOSAP.Text);
                double qtyNetto = Convert.ToDouble(tbQtyNetto.Text);

                string docType = cbDocType.Text;

                if (docType == "GRPO")
                {
                    UpdateGRPO(docEntryDOSAP, lineNumDOSAP, qtyNetto);
                    PrintWeighbridgeTicket(tiketNum);
                }
                else
                {
                    UpdateDO(docEntryDOSAP, lineNumDOSAP, qtyNetto);
                    PrintWeighbridgeTicket(tiketNum);
                }

                ClearAllField();
                DisableFieldWB();
                tbBaseDoc.Focus();
                btnCariBaseDoc.Visible = true;
                btnRefreshData.Visible = false;
            }
        }

        private void fillFormFromDataRow(DataRow row)
        {

            int docEntry = Convert.ToInt32(row["U_SOL_DOC_ENTRY_SAP"]);
            string docNum = row["U_SOL_DOCNUM_SAP"].ToString();
            noKontrak = row["U_SOL_NO_KONTRAK"] != DBNull.Value ? row["U_SOL_NO_KONTRAK"].ToString() : "";
            int linenumSAP = Convert.ToInt32(row["U_SOL_LINENUM_SAP"]);

            if (row["U_SOL_TANGGAL_DOKUMEN"] == DBNull.Value)
            {
                dtTglDokPabean.CustomFormat = " ";
            }
            else
            {
                dtTglDokPabean.Value = Convert.ToDateTime(row["U_SOL_TANGGAL_DOKUMEN"]);
            }

            //lblValDocNo.Text = ": " + docNum;
            //lblValKontrak.Text = ": " + noKontrak;
            cbDocType.Text = row["U_SOL_DOC_TYPE"].ToString();
            tbBaseDoc.Text = row["U_SOL_DOCNUM_SAP"].ToString();
            tbTiketNum.Text = row["U_SOL_TIKET_WB"].ToString();
            //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
            //lblValKontrak.Text = ": " + reader["U_SOL_NO_KONTRAK"].ToString();
            lblValStatusWB.Text = row["U_SOL_STATUS"].ToString();
            tbNopol.Text = row["U_SOL_NOPOL"].ToString();
            tbNamaSupir.Text = row["U_SOL_NAMA_SUPIR"].ToString();
            tbSIMSupir.Text = row["U_SOL_NO_SIM_SUPIR"].ToString();
            tbJnsTruck.Text = row["U_SOL_JENIS_TRUCK"].ToString();
            tbCommodity.Text = row["U_SOL_COMMODITY"].ToString();
            tbSealNo.Text = row["U_SOL_SEAL_NO"].ToString();
            tbDONo.Text = row["U_SOL_DO_NO"].ToString();
            tbNote.Text = row["U_SOL_NOTE"].ToString();
            tbKodeDokPabean.Text = row["U_SOL_KODE_DOKUMEN"].ToString();
            tbNoDokPabean.Text = row["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
            tbNomorSPB.Text = row["U_SOL_NOMOR_SPB"].ToString();
            tbNomorAju.Text = row["U_SOL_NOMOR_AJU"].ToString();
            tbContNo1.Text = row["U_SOL_CONTAINER_1_NO"].ToString();
            tbContNo2.Text = row["U_SOL_CONTAINER_2_NO"].ToString();
            tbQtyMasuk.Text = Convert.ToDecimal(row["U_SOL_BERAT_MASUK"]).ToString("0.##");
            tbQtyKeluar.Text = Convert.ToDecimal(row["U_SOL_BERAT_KELUAR"]).ToString("0.##");
            tbQtyNetto.Text = Convert.ToDecimal(row["U_SOL_BERAT_NETTO"]).ToString("0.##");
            lblValDocEntDOSAP.Text = docEntry.ToString();
            lblValLineNumDOSAP.Text = linenumSAP.ToString();
            valTanggalMasuk.Text = row["U_SOL_TANGGAL_MASUK"].ToString();
            valWaktuMasuk.Text = row["U_SOL_WAKTU_MASUK"].ToString();
            tbTransporterInt.Text = row["U_SOL_TRANSPORTER_INT"].ToString();
            tbTransporterEks.Text = row["U_SOL_TRANSPORTER_EKS"].ToString();
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
            cbTKManual.Enabled = true;
            tbSealNo.Enabled = true;
            tbDONo.Enabled = true;
            tbNote.Enabled = true;
            tbKodeDokPabean.Enabled = true;
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

        }

        private void DisableFieldWB()
        {
            //cbDocType.Enabled = false;
            //tbBaseDoc.Enabled = false;
            tbNopol.Enabled = false;
            tbNamaSupir.Enabled = false;
            tbSIMSupir.Enabled = false;
            tbJnsTruck.Enabled = false;
            cbTKManual.Enabled = false;
            btnTimbangKeluar.Enabled = false;
            //btnSubmitUjiLab.Enabled = false;
            btnWBSave.Enabled = false;
            tbNoDokPabean.Enabled = false;
            tbKodeDokPabean.Enabled = false;
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
            dtTglDokPabean.Checked = false;
            dtTglDokPabean.Value = DateTime.Now;
            cbTKManual.Checked = false;
            tbTransporterInt.Text = String.Empty;
            tbTransporterEks.Text = String.Empty;
            noKontrak = "";
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
                        }
                        else
                        {
                            MessageBox.Show($"Document SAP ditemukan. Total {result.Rows.Count} tiket. Pilih salah satu tiket.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            using (SelectTicketForm selectTicketForm = new SelectTicketForm(result))
                            {
                                if (selectTicketForm.ShowDialog(this) == DialogResult.OK && selectTicketForm.SelectedTicket != null)
                                {
                                    fillFormFromDataRow(selectTicketForm.SelectedTicket);

                                    EnableFieldWB();
                                    btnCariBaseDoc.Visible = false;
                                    btnRefreshData.Visible = true;
                                    lblStatusWB.Visible = true;
                                    lblValStatusWB.Visible = true;
                                    tbTiketNum.Enabled = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    string noTiket = tbTiketNum.Text;

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
                                        
                                        cbDocType.Text = reader["U_SOL_DOC_TYPE"].ToString();
                                        tbBaseDoc.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
                                        //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
                                        tbBaseDoc.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
                                        lblValDocEntDOSAP.Text = reader["U_SOL_DOC_ENTRY_SAP"].ToString();
                                        lblValLineNumDOSAP.Text = reader["U_SOL_LINENUM_SAP"].ToString();
                                        lblValStatusWB.Text = reader["U_SOL_STATUS"].ToString();
                                        noKontrak = reader["U_SOL_NO_KONTRAK"] != DBNull.Value ? reader["U_SOL_NO_KONTRAK"].ToString() : "";
                                        tbNopol.Text = reader["U_SOL_NOPOL"].ToString();
                                        tbNamaSupir.Text = reader["U_SOL_NAMA_SUPIR"].ToString();
                                        tbSIMSupir.Text = reader["U_SOL_NO_SIM_SUPIR"].ToString();
                                        tbJnsTruck.Text = reader["U_SOL_JENIS_TRUCK"].ToString();
                                        tbCommodity.Text = reader["U_SOL_COMMODITY"].ToString();
                                        tbSealNo.Text = reader["U_SOL_SEAL_NO"].ToString();
                                        tbDONo.Text = reader["U_SOL_DO_NO"].ToString();
                                        tbNote.Text = reader["U_SOL_NOTE"].ToString();
                                        tbKodeDokPabean.Text = reader["U_SOL_KODE_DOKUMEN"].ToString();
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
        // function inilah yang mengharuskan split kontrak
        private void UpdateGRPO(int docEntryDOSAP, int lineNumDOSAP, double qtyNetto)
        {

            try
            {
                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                oSBOConnection.oCompany.StartTransaction();

                SAPbobsCOM.Documents oGRPO = null;
                oGRPO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);

                oGRPO.GetByKey(docEntryDOSAP);

                oGRPO.Lines.SetCurrentLine(Convert.ToInt32(lineNumDOSAP));
                oGRPO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyNetto;

                int retval = 0;

                retval = oGRPO.Update();

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
                oSBOConnection.oCompany.Disconnect();
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                ClearAllField();
            }
        }

        private void UpdateDO(int docEntryDOSAP, int lineNumDOSAP, double qtyNetto)
        {

            try
            {
                oSBOConnection.connectSBOS(ConfigManager.sboConfig);

                oSBOConnection.oCompany.StartTransaction();

                SAPbobsCOM.Documents oDO = null;
                oDO = oSBOConnection.oCompany.GetBusinessObject(BoObjectTypes.oDeliveryNotes);

                oDO.GetByKey(docEntryDOSAP);

                oDO.Lines.SetCurrentLine(Convert.ToInt32(lineNumDOSAP));
                oDO.Lines.UserFields.Fields.Item("U_SOL_QTY_WB").Value = qtyNetto;

                int retval = 0;

                retval = oDO.Update();

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
                oSBOConnection.oCompany.Disconnect();
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);

                ClearAllField();
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
                oGeneralData.SetProperty("U_SOL_BERAT_KELUAR", Convert.ToDouble(tbQtyKeluar.Text));
                oGeneralData.SetProperty("U_SOL_BERAT_NETTO", Convert.ToDouble(tbQtyNetto.Text));
                oGeneralData.SetProperty("U_SOL_KODE_DOKUMEN", tbKodeDokPabean.Text);
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
                oGeneralData.SetProperty("U_SOL_NOTE", tbNote.Text);
                oGeneralData.SetProperty("U_SOL_USER", lblUsername.Text);
                oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
                oGeneralData.SetProperty("U_SOL_WEIGHOUT", wbWeighout);
                oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumDOSAP.Text));

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
                if (oSBOConnection.oCompany != null && oSBOConnection.oCompany.InTransaction)
                {
                    oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
                }

                oSBOConnection.oCompany.Disconnect();

                //MessageBox.Show("Data updated. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                MessageBox.Show("Data Synced to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                if (oSBOConnection.oCompany != null && oSBOConnection.oCompany.InTransaction)
                {
                    oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_RollBack);
                }
                if (oSBOConnection.oCompany != null && oSBOConnection.oCompany.Connected)
                {
                    oSBOConnection.oCompany.Disconnect();
                }
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTimbangKeluar_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (!_serialPort.IsOpen)
                    _serialPort.Open();

                _serialPort.DiscardInBuffer();

                // Show reading dialog
                ShowReadingDialog("Timbang Keluar");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
