using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Windows.Forms;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;

namespace Weighbridge
{
    public partial class ReprintTiket : Form
    {
        HANAConnetion hanaConn = new HANAConnetion();
        SBOConnection oSBOConnection = new SBOConnection();

        public ReprintTiket()
        {
            InitializeComponent();
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                ClearAllField();
                btnRefreshData.Visible = false;
                btnCariBaseDoc.Visible = true;
                lblStatusWB.Visible = false;
                lblValStatusWB.Visible = false;
                tbTiketNum.Enabled = true;
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

        //private void ShowPrintPreview(string ticketNumber)
        //{
        //    ReportDocument reportDoc = null;

        //    try
        //    {
        //        reportDoc = new ReportDocument();
        //        reportDoc.Load(Application.StartupPath + @"\Reports\WeighbridgeTicket.rpt");

        //        // Pass ticket number as parameter
        //        reportDoc.SetParameterValue("TicketNumber", ticketNumber);

        //        // --- OR if using DataSet ---
        //        // DataSet ds = GetTicketData(ticketNumber);
        //        // if (ds.Tables[0].Rows.Count == 0)
        //        // {
        //        //     MessageBox.Show("Ticket not found.");
        //        //     return;
        //        // }
        //        // reportDoc.SetDataSource(ds);

        //        // Open preview form maximized
        //        using (FormPrintPreview previewForm = new FormPrintPreview(reportDoc))
        //        {
        //            previewForm.Text = $"Print Preview - Ticket {ticketNumber}";
        //            previewForm.WindowState = FormWindowState.Maximized;
        //            previewForm.ShowDialog();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Failed to open preview:\n{ex.Message}", "Error",
        //            MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //    finally
        //    {
        //        if (reportDoc != null)
        //        {
        //            reportDoc.Close();
        //            reportDoc.Dispose();
        //        }
        //    }
        //}

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
                        }
                        else if (result.Rows.Count == 1)
                        {
                            MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            fillFormFromDataRow(result.Rows[0]);;
                            btnCariBaseDoc.Visible = false;
                            btnRefreshData.Visible = true;
                            lblStatusWB.Visible = true;
                            lblValStatusWB.Visible = true;
                            tbTiketNum.Enabled = false;
                            btnWBReprint.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show($"Document SAP ditemukan. Total {result.Rows.Count} tiket. Pilih salah satu tiket.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            using (SelectTicketForm selectTicketForm = new SelectTicketForm(result))
                            {
                                if (selectTicketForm.ShowDialog(this) == DialogResult.OK && selectTicketForm.SelectedTicket != null)
                                {
                                    fillFormFromDataRow(selectTicketForm.SelectedTicket);

                                    btnCariBaseDoc.Visible = false;
                                    btnRefreshData.Visible = true;
                                    lblStatusWB.Visible = true;
                                    lblValStatusWB.Visible = true;
                                    tbTiketNum.Enabled = false;
                                    btnWBReprint.Enabled = true;
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
                                }
                                else
                                {
                                    while (reader.Read())
                                    {
                                        MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        btnWBReprint.Enabled = true;

                                        if (reader["U_SOL_TANGGAL_DOKUMEN"] == DBNull.Value)
                                        {
                                            dtTglDokPabean.CustomFormat = " ";
                                        }
                                        else
                                        {
                                            dtTglDokPabean.Value = Convert.ToDateTime(reader["U_SOL_TANGGAL_DOKUMEN"]);
                                        }

                                        cbDocType.Text = reader["U_SOL_DOC_TYPE"].ToString();
                                        tbBaseDoc.Text = reader["U_SOL_VENDOR_REF"].ToString();
                                        //lblValDocNo.Text = ": " + reader["U_SOL_DOCNUM_SAP"].ToString();
                                        tbBaseSAP.Text = reader["U_SOL_DOCNUM_SAP"].ToString();
                                        lbValDocEntSAP.Text = reader["U_SOL_DOC_ENTRY_SAP"].ToString();
                                        lblValLineNumSAP.Text = reader["U_SOL_LINENUM_SAP"].ToString();
                                        lblValStatusWB.Text = reader["U_SOL_STATUS"].ToString();
                                        //lblValKontrak.Text = ": " + reader["U_SOL_NO_KONTRAK"].ToString();
                                        tbNopol.Text = reader["U_SOL_NOPOL"].ToString();
                                        tbNamaSupir.Text = reader["U_SOL_NAMA_SUPIR"].ToString();
                                        tbSIMSupir.Text = reader["U_SOL_NO_SIM_SUPIR"].ToString();
                                        tbJnsTruck.Text = reader["U_SOL_JENIS_TRUCK"].ToString();
                                        tbBrutoKebun.Text = reader["U_SOL_BRUTO_KEBUN"].ToString();
                                        tbTaraKebun.Text = reader["U_SOL_TARA_KEBUN"].ToString();
                                        tbNettoKebun.Text = reader["U_SOL_NETTO_KEBUN"].ToString();
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
                                        tbKodeDokPabean.Text = reader["U_SOL_KODE_DOKUMEN"].ToString();
                                        tbTangkiNo.Text = reader["U_SOL_NOMOR_TANGKI"].ToString();
                                        tbNoDokPabean.Text = reader["U_SOL_NO_DOKUMEN_PABEAN"].ToString();
                                        tbNomorSPB.Text = reader["U_SOL_NOMOR_SPB"].ToString();
                                        tbNomorAju.Text = reader["U_SOL_NOMOR_AJU"].ToString();
                                        tbContNo1.Text = reader["U_SOL_CONTAINER_1_NO"].ToString();
                                        tbContNo2.Text = reader["U_SOL_CONTAINER_2_NO"].ToString();
                                        tbQtyMasuk.Text = Convert.ToDecimal(reader["U_SOL_BERAT_MASUK"]).ToString("0.##");
                                        tbQtyKeluar.Text = Convert.ToDecimal(reader["U_SOL_BERAT_KELUAR"]).ToString("0.##");
                                        tbQtyNetto.Text = Convert.ToDecimal(reader["U_SOL_BERAT_NETTO"]).ToString("0.##");
                                        lblValTanggalMasuk.Text = reader["U_SOL_TANGGAL_MASUK"].ToString();
                                        lblValWaktuMasuk.Text = reader["U_SOL_WAKTU_MASUK"].ToString();
                                        tbTransporterInt.Text = reader["U_SOL_TRANSPORTER_INT"].ToString();
                                        tbTransporterEks.Text = reader["U_SOL_TRANSPORTER_EKS"].ToString();

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
        }

        private void fillFormFromDataRow(DataRow row)
        {

            int docEntry = Convert.ToInt32(row["U_SOL_DOC_ENTRY_SAP"]);
            string docNum = row["U_SOL_DOCNUM_SAP"].ToString();
            string noKontrak = row["U_SOL_NO_KONTRAK"].ToString();
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
            tbBrutoKebun.Text = row["U_SOL_BRUTO_KEBUN"].ToString();
            tbTaraKebun.Text = row["U_SOL_TARA_KEBUN"].ToString();
            tbNettoKebun.Text = row["U_SOL_NETTO_KEBUN"].ToString();
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
            tbKodeDokPabean.Text = row["U_SOL_KODE_DOKUMEN"].ToString();
            tbTangkiNo.Text = row["U_SOL_NOMOR_TANGKI"].ToString();
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
            lblValTanggalMasuk.Text = row["U_SOL_TANGGAL_MASUK"].ToString();
            lblValWaktuMasuk.Text = row["U_SOL_WAKTU_MASUK"].ToString();
            tbTransporterInt.Text = row["U_SOL_TRANSPORTER_INT"].ToString();
            tbTransporterEks.Text = row["U_SOL_TRANSPORTER_EKS"].ToString();
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
            tbTransporterInt.Text = String.Empty;
            tbTransporterEks.Text = String.Empty;
        }

        private void btnWBReprint_Click(object sender, EventArgs e)
        {
            string tiketNumber = tbTiketNum.Text;

            oSBOConnection.connectSBOS(ConfigManager.sboConfig);

            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);
            var connection = new HanaConnection(connectionString);

            string code = "";
            string keluar = "";
            string waktukeluar = "";

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_SEARCH_CODE_UDO('" + tiketNumber + "')";

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
                                keluar = reader["KELUAR"].ToString();
                                waktukeluar = reader["WAKTUKELUAR"].ToString();

                            }
                        }
                    }
                }

                connection.Close();
            }

            ////Declare all SAPbobsCOM untuk DI API UDO
            SAPbobsCOM.GeneralService oGeneralService;
            SAPbobsCOM.GeneralData oGeneralData;
            SAPbobsCOM.GeneralDataParams oGeneralParams;

            SAPbobsCOM.CompanyService oSTR = null;
            oSTR = oSBOConnection.oCompany.GetCompanyService();

            ////Get a handle to the Stock Request UDO
            //oGeneralService = oSTR.GetGeneralService("OWB");
            ////oGeneralParams = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);

            ////oGeneralParams.SetProperty("Code", code);
            ////oGeneralData = oGeneralService.GetByParams(oGeneralParams);

            ////Specify data for main UDO (Header)
            //oGeneralData = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);

            //oGeneralData.SetProperty("Code", code); oGeneralData.SetProperty("U_SOL_DOC_TYPE", cbDocType.SelectedItem.ToString());
            //oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", tbBaseSAP.Text);
            //oGeneralData.SetProperty("U_SOL_VENDOR_REF", tbBaseDoc.Text);
            //oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lbValDocEntSAP.Text));
            //oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
            //oGeneralData.SetProperty("U_SOL_COMMODITY", tbCommodity.Text);
            ////oGeneralData.SetProperty("U_SOL_NO_KONTRAK", lblValKontrak.Text);
            //oGeneralData.SetProperty("U_SOL_NOPOL", tbNopol.Text);
            //oGeneralData.SetProperty("U_SOL_NAMA_SUPIR", tbNamaSupir.Text);
            //oGeneralData.SetProperty("U_SOL_NO_SIM_SUPIR", tbSIMSupir.Text);
            //oGeneralData.SetProperty("U_SOL_JENIS_TRUCK", tbJnsTruck.Text);
            //oGeneralData.SetProperty("U_SOL_BERAT_MASUK", Convert.ToDouble(tbQtyMasuk.Text));
            //oGeneralData.SetProperty("U_SOL_BERAT_KELUAR", Convert.ToDouble(tbQtyKeluar.Text));
            //oGeneralData.SetProperty("U_SOL_BERAT_NETTO", Convert.ToDouble(tbQtyNetto.Text));
            //oGeneralData.SetProperty("U_SOL_BRUTO_KEBUN", tbBrutoKebun.Text);
            //oGeneralData.SetProperty("U_SOL_TARA_KEBUN", tbTaraKebun.Text);
            //oGeneralData.SetProperty("U_SOL_NETTO_KEBUN", tbNettoKebun.Text);
            //oGeneralData.SetProperty("U_SOL_FFA_KEBUN", tbFFAKebun.Text);
            //oGeneralData.SetProperty("U_SOL_MOIST_KEBUN", tbMOIST.Text);
            //oGeneralData.SetProperty("U_SOL_LAB_FFA", tbFFA.Text);
            //oGeneralData.SetProperty("U_SOL_LAB_MOIST", tbMOIST.Text);
            //oGeneralData.SetProperty("U_SOL_LAB_IMP", tbIMP.Text);
            //oGeneralData.SetProperty("U_SOL_LAB_DOBI", tbDOBI.Text);
            //oGeneralData.SetProperty("U_SOL_LAB_CAROTINE", tbCAROTINE.Text);
            //oGeneralData.SetProperty("U_SOL_KODE_DOKUMEN", tbKodeDokPabean.Text);
            //oGeneralData.SetProperty("U_SOL_TANGGAL_DOKUMEN", dtTglDokPabean.Checked ? dtTglDokPabean.Value.ToString("yyyyMMdd") : "");
            //oGeneralData.SetProperty("U_SOL_NO_DOKUMEN", tbNoDokPabean.Text);
            //oGeneralData.SetProperty("U_SOL_NOMOR_SPB", tbNomorSPB.Text);
            //oGeneralData.SetProperty("U_SOL_NOMOR_AJU", tbNomorAju.Text);
            //oGeneralData.SetProperty("U_SOL_TANGGAL_MASUK", Convert.ToDateTime(lblValTanggalMasuk.Text));
            //oGeneralData.SetProperty("U_SOL_WAKTU_MASUK", Convert.ToDateTime(lblValWaktuMasuk.Text));
            //oGeneralData.SetProperty("U_SOL_TANGGAL_KELUAR", Convert.ToDateTime(keluar));
            ////oGeneralData.SetProperty("U_SOL_WAKTU_KELUAR", Convert.ToDateTime(keluar));
            //string cleanTimeKeluar = keluar.Replace(":", ""); // Asumsi format HH:mm -> HHmm
            //if (int.TryParse(cleanTimeKeluar, out int sapTime))
            //{
            //    oGeneralData.SetProperty("U_SOL_WAKTU_KELUAR", sapTime);
            //}

            //oGeneralData.SetProperty("U_SOL_SEAL_NO", tbSealNo.Text);
            //oGeneralData.SetProperty("U_SOL_DO_NO", tbDONo.Text);
            //oGeneralData.SetProperty("U_SOL_CONTAINER_1_NO", tbContNo1.Text);
            //oGeneralData.SetProperty("U_SOL_CONTAINER_2_NO", tbContNo2.Text);
            //oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", tbTransporterInt.Text);
            //oGeneralData.SetProperty("U_SOL_TRANSPORTER_EKS", tbTransporterEks.Text);
            //oGeneralData.SetProperty("U_SOL_NOMOR_TANGKI", tbTangkiNo.Text);
            //oGeneralData.SetProperty("U_SOL_NOTE", tbNote.Text);
            //oGeneralData.SetProperty("U_SOL_USER", lblUsername.Text);
            //oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
            //oGeneralData.SetProperty("U_SOL_REPRINT", wbReprint);
            //oGeneralData.SetProperty("U_SOL_LINENUM_SAP", Convert.ToInt32(lblValLineNumSAP.Text));

            //oGeneralService.Update(oGeneralData);
            ////oSBOConnection.oCompany.EndTransaction(SAPbobsCOM.BoWfTransOpt.wf_Commit);
            ///
            oGeneralService = oSTR.GetGeneralService("OWB");
            oGeneralParams = oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);

            oGeneralParams = (SAPbobsCOM.GeneralDataParams)oGeneralService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);
            oGeneralParams.SetProperty("Code", code);

            oGeneralData = oGeneralService.GetByParams(oGeneralParams);

            oGeneralData.SetProperty("U_SOL_REPRINT", "Y");

            // 4. Eksekusi Update
            oGeneralService.Update(oGeneralData);

            oSBOConnection.oCompany.Disconnect();

            //UpdateWBUDO(tbTiketNum.Text, wbStatus, wbReprint);
            PrintWeighbridgeTicket(tiketNumber);
        }

        private void UpdateWBUDO(string tiketNum, string wbStatus, string wbReprint)
        {
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);
            var connection = new HanaConnection(connectionString);

            string code = "";
            string docnum = "";
            string vendor = "";
            string keluar = "";
            string waktukeluar = "";

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
                                docnum = reader["DOCNUM"].ToString();
                                vendor = reader["VENDOR"].ToString();
                                keluar = reader["KELUAR"].ToString();
                                waktukeluar = reader["WAKTUKELUAR"].ToString();
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
                oGeneralData.SetProperty("U_SOL_DOCNUM_SAP", docnum);
                oGeneralData.SetProperty("U_SOL_VENDOR_REF", vendor);
                oGeneralData.SetProperty("U_SOL_DOC_ENTRY_SAP", Convert.ToInt32(lbValDocEntSAP.Text));
                oGeneralData.SetProperty("U_SOL_TIKET_WB", tbTiketNum.Text);
                oGeneralData.SetProperty("U_SOL_COMMODITY", tbCommodity.Text);
                //oGeneralData.SetProperty("U_SOL_NO_KONTRAK", lblValKontrak.Text);
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
                oGeneralData.SetProperty("U_SOL_MOIST_KEBUN", tbMOIST.Text);
                oGeneralData.SetProperty("U_SOL_LAB_FFA", tbFFA.Text);
                oGeneralData.SetProperty("U_SOL_LAB_MOIST", tbMOIST.Text);
                oGeneralData.SetProperty("U_SOL_LAB_IMP", tbIMP.Text);
                oGeneralData.SetProperty("U_SOL_LAB_DOBI", tbDOBI.Text);
                oGeneralData.SetProperty("U_SOL_LAB_CAROTINE", tbCAROTINE.Text);
                oGeneralData.SetProperty("U_SOL_KODE_DOKUMEN", tbKodeDokPabean.Text);
                oGeneralData.SetProperty("U_SOL_TANGGAL_DOKUMEN", dtTglDokPabean.Checked ? dtTglDokPabean.Value.ToString("yyyyMMdd") : "");
                oGeneralData.SetProperty("U_SOL_NO_DOKUMEN", tbNoDokPabean.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_SPB", tbNomorSPB.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_AJU", tbNomorAju.Text);
                oGeneralData.SetProperty("U_SOL_TANGGAL_MASUK", Convert.ToDateTime(lblValTanggalMasuk.Text));
                oGeneralData.SetProperty("U_SOL_WAKTU_MASUK", Convert.ToDateTime(lblValWaktuMasuk.Text));
                oGeneralData.SetProperty("U_SOL_TANGGAL_KELUAR", keluar);
                oGeneralData.SetProperty("U_SOL_WAKTU_KELUAR", waktukeluar);
                oGeneralData.SetProperty("U_SOL_SEAL_NO", tbSealNo.Text);
                oGeneralData.SetProperty("U_SOL_DO_NO", tbDONo.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_1_NO", tbContNo1.Text);
                oGeneralData.SetProperty("U_SOL_CONTAINER_2_NO", tbContNo2.Text);
                oGeneralData.SetProperty("U_SOL_TRANSPORTER_INT", tbTransporterInt.Text);
                oGeneralData.SetProperty("U_SOL_TRANSPORTER_EKS", tbTransporterEks.Text);
                oGeneralData.SetProperty("U_SOL_NOMOR_TANGKI", tbTangkiNo.Text);
                oGeneralData.SetProperty("U_SOL_NOTE", tbNote.Text);
                oGeneralData.SetProperty("U_SOL_USER", lblUsername.Text);
                oGeneralData.SetProperty("U_SOL_STATUS", wbStatus);
                oGeneralData.SetProperty("U_SOL_REPRINT", wbReprint);
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

        private void ReprintTiket_Load(object sender, EventArgs e)
        {
            lblUsername.Text = SessionUser.username;
        }
    }
}
