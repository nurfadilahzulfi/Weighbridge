using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Weighbridge.Core.Config;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class TimbangKeluarForm : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;
        private readonly IWeighOutService _weighOutService;
        private readonly IPabeanService _pabeanService;
        private readonly IPrintService _printService;
        private readonly SerialPortService _serialPortService;

        private WeighStatusForm _statusForm;

        public string timbangType = "A";
        private string noKontrak = "";

        public TimbangKeluarForm()
            : this(new HanaRepository(), new SapRepository(), new PrintService(), new SerialPortService())
        {
        }

        public TimbangKeluarForm(
            IHanaRepository hanaRepo,
            ISapRepository sapRepo,
            IPrintService printService,
            SerialPortService serialPortService)
        {
            InitializeComponent();
            _hanaRepo = hanaRepo ?? new HanaRepository();
            _sapRepo = sapRepo ?? new SapRepository();
            _printService = printService ?? new PrintService(_hanaRepo);
            _serialPortService = serialPortService ?? new SerialPortService();
            _weighOutService = new WeighOutService(_hanaRepo, _sapRepo);
            _pabeanService = new PabeanService(_hanaRepo);
        }

        private void TImbangKeluarForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataKodeDokumenPabean();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;

            dtTglDokPabean.ShowCheckBox = true;
            dtTglDokPabean.Checked = false;

            cbTKManual.Visible = AppFeatures.AllowManualWeighing;
            cbTKManual.Enabled = false;
            cbTKManual.Checked = false;
            timbangType = "A";

            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.BringToFront();
            }
            ResponsiveLayoutHelper.Attach(this);
            WatermarkHelper.Apply(this);
            LoadPendingTickets(silent: true);
        }

        private void InitializeSerialPort()
        {
            _serialPortService.Initialize();

            _serialPortService.OnWeightReceived += (weight) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    tbQtyKeluar.Text = weight;
                    _statusForm?.ShowSuccess(weight);
                });
            };

            _serialPortService.OnError += (errorMsg) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    _statusForm?.Close();
                    MessageBox.Show($"Read error: {errorMsg}");
                });
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _serialPortService?.Dispose();
            base.OnFormClosing(e);
        }

        private void LoadDataKodeDokumenPabean()
        {
            cmbKodeDokPabean.Items.Clear();
            var kodeDokumen = _hanaRepo.GetKodeDokumenPabean();
            foreach (var kode in kodeDokumen)
            {
                cmbKodeDokPabean.Items.Add(kode);
            }
        }

        private void LoadDataNomorTangki(string itemName)
        {
            cmbTangkiNo.Items.Clear();
            if (string.IsNullOrWhiteSpace(itemName)) return;

            var tangkiList = _hanaRepo.GetNomorTangki(itemName);
            foreach (var tangki in tangkiList)
            {
                cmbTangkiNo.Items.Add(tangki);
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

        private void cbTKManual_CheckedChanged(object sender, EventArgs e)
        {
            bool manual = cbTKManual.Checked;
            timbangType = manual ? "M" : "A";
            tbQtyKeluar.Enabled = manual;
            tbQtyKeluar.Text = "0";
            btnTimbangKeluar.Visible = !manual;
        }

        private void btnTimbangKeluar_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPortService.StartReading();
                ShowReadingDialog("Weigh Out");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void tbQtyKeluar_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != ','))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == ',') && ((sender as TextBox).Text.IndexOf(',') > -1))
            {
                e.Handled = true;
            }
        }

        private void tbBaseDoc_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void tbQtyKeluar_TextChanged(object sender, EventArgs e)
        {
            double qtyMasuk = DataConversionHelper.SafeToDouble(tbQtyMasuk.Text);
            double qtyKeluar = DataConversionHelper.SafeToDouble(tbQtyKeluar.Text);
            double qtyNetto = qtyKeluar > 0 ? _weighOutService.CalculateNetto(qtyMasuk, qtyKeluar) : 0;
            tbQtyNetto.Text = qtyNetto.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void PrintWeighbridgeTicket(string ticketNumber)
        {
            if (_printService.PrintTicket(ticketNumber, TicketPrintType.Keluar, out string error))
            {
                MessageBox.Show($"Ticket {ticketNumber} sent to printer.", "Print Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Failed to print ticket:\n{error}", "Print Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCariBaseDoc_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(tbBaseDoc.Text) && string.IsNullOrEmpty(tbTiketNum.Text))
            {
                MessageBox.Show("Base Document atau Nomor Tiket harus diisi.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!string.IsNullOrEmpty(tbBaseDoc.Text) && !string.IsNullOrEmpty(tbTiketNum.Text))
            {
                MessageBox.Show("Hanya bisa mencari dari Base Document atau Nomor Tiket.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrEmpty(cbDocType.Text) && !string.IsNullOrEmpty(tbBaseDoc.Text))
            {
                MessageBox.Show("Pilih Document Type terlebih dahulu", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!string.IsNullOrEmpty(tbBaseDoc.Text))
            {
                string docType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text);
                string docNum = tbBaseDoc.Text;

                if (string.IsNullOrEmpty(docType) || string.IsNullOrEmpty(docNum))
                {
                    MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DataTable result = _hanaRepo.SearchBaseDocJoinWbData(docType, docNum);
                if (result == null || result.Rows.Count == 0)
                {
                    // SP di TANIMASLIVE hanya mencari No. PO/SO: coba sebagai No. Tiket, lalu sebagai Vendor Ref
                    if (_hanaRepo.SearchTicketByTiketNum(docNum.Trim()).Rows.Count > 0)
                    {
                        tbBaseDoc.Text = string.Empty;
                        SearchByTiketNum(docNum.Trim());
                        tbTransporterInt.Enabled = false;
                        return;
                    }
                    var exact = DocumentLookupFilter.FindExact(new DocumentLookupService(_hanaRepo).GetOpenDocuments(docType), docNum);
                    if (exact != null && !string.Equals(exact.DocNum, docNum.Trim(), StringComparison.OrdinalIgnoreCase))
                        result = _hanaRepo.SearchBaseDocJoinWbData(docType, exact.DocNum);
                }
                if (result == null || result.Rows.Count == 0)
                {
                    MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DisableFieldWB();
                    ClearAllField();
                }
                else if (result.Rows.Count == 1)
                {
                    MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    FillFormFromDataRow(result.Rows[0]);
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
                            FillFormFromDataRow(selectTicketForm.SelectedTicket);
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
            else
            {
                SearchByTiketNum(tbTiketNum.Text);
            }

            tbTransporterInt.Enabled = false;
        }

        private void SearchByTiketNum(string noTiket)
        {
            if (string.IsNullOrWhiteSpace(noTiket))
            {
                MessageBox.Show("Nomor Tiket tidak boleh kosong.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable dtTiket = _hanaRepo.SearchTicketByTiketNum(noTiket);
            if (dtTiket.Rows.Count == 0)
            {
                MessageBox.Show("Nomor Tiket tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableFieldWB();
                ClearAllField();
            }
            else
            {
                // Semua record tiket (termasuk K1/K2 split) harus masih WEIGHIN
                if (!ValidateAllRowsWeighIn(dtTiket, noTiket))
                {
                    DisableFieldWB();
                    ClearAllField();
                    return;
                }

                if (dtTiket.Rows.Count == 1)
                {
                    ClearSplitRows();

                FillFormFromDataRow(dtTiket.Rows[0]);

                EnableFieldWB();
                btnCariBaseDoc.Visible = false;
                btnRefreshData.Visible = true;
                lblStatusWB.Visible = true;
                lblValStatusWB.Visible = true;
                tbTiketNum.Enabled = false;

                MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SetSplitRows(dtTiket);

                FillFormFromDataRow(_splitRows[0]);

                EnableFieldWB();
                btnCariBaseDoc.Visible = false;
                btnRefreshData.Visible = true;
                lblStatusWB.Visible = true;
                lblValStatusWB.Visible = true;
                lblValStatusWB.Text = _splitRows[0]["U_SOL_STATUS"].ToString() + SplitStatusSuffix;
                tbTiketNum.Enabled = false;

                var info = new System.Text.StringBuilder();
                info.Append($"Nomor Tiket ditemukan (MODE SPLIT KONTRAK {_splitRows.Count} PO):\n\n");
                for (int i = 0; i < _splitRows.Count; i++)
                {
                    DataRow r = _splitRows[i];
                    string doc = r["U_SOL_DOCNUM_SAP"]?.ToString() ?? "";
                    string kon = r["U_SOL_NO_KONTRAK"]?.ToString() ?? "";
                    double nettoKebun = DataConversionHelper.SafeToDouble(r["U_SOL_NETTO_KEBUN"]);
                    info.Append($"  Kontrak K{i + 1}: Doc {doc} | No. Kontrak: {(string.IsNullOrEmpty(kon) ? "-" : kon)} | Netto: {nettoKebun:N0} kg\n");
                }
                info.Append($"\nSaat timbang keluar disimpan, sistem akan otomatis merilis {JumlahGRPOText(_splitRows.Count)} GRPO ke SAP.");

                MessageBox.Show(info.ToString(), "Tiket Split Kontrak Ditemukan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void FillFormFromDataRow(DataRow row)
        {
            int docEntry = DataConversionHelper.SafeToInt(row["U_SOL_DOC_ENTRY_SAP"]);
            int linenumSAP = DataConversionHelper.SafeToInt(row["U_SOL_LINENUM_SAP"]);

            if (row["U_SOL_TANGGAL_DOKUMEN"] == DBNull.Value)
                ClearTanggalPabean();
            else
                SetTanggalPabean(Convert.ToDateTime(row["U_SOL_TANGGAL_DOKUMEN"]));

            if (row["U_SOL_KODE_DOKUMEN"] == DBNull.Value)
            {
                LoadDataKodeDokumenPabean();
            }
            else
            {
                cmbKodeDokPabean.Text = row["U_SOL_KODE_DOKUMEN"].ToString();
            }

            string commodity = row["U_SOL_COMMODITY"] != null && row["U_SOL_COMMODITY"] != DBNull.Value
                ? row["U_SOL_COMMODITY"].ToString()
                : "";
            tbCommodity.Text = commodity;

            LoadDataNomorTangki(commodity);

            if (row["U_SOL_NOMOR_TANGKI"] != null && row["U_SOL_NOMOR_TANGKI"] != DBNull.Value)
            {
                cmbTangkiNo.Text = row["U_SOL_NOMOR_TANGKI"].ToString();
            }

            string docTypeVal = row["U_SOL_DOC_TYPE"] != null && row["U_SOL_DOC_TYPE"] != DBNull.Value ? row["U_SOL_DOC_TYPE"].ToString() : "PO";
            cbDocType.Text = docTypeVal;
            cbDocType.SelectedItem = docTypeVal;
            tbBaseSAP.Text = row["U_SOL_DOCNUM_SAP"] != null && row["U_SOL_DOCNUM_SAP"] != DBNull.Value ? row["U_SOL_DOCNUM_SAP"].ToString() : "";
            string vendorRef = row.Table.Columns.Contains("U_SOL_VENDOR_REF") && row["U_SOL_VENDOR_REF"] != DBNull.Value ? row["U_SOL_VENDOR_REF"].ToString() : "";
            tbBaseDoc.Text = DataConversionHelper.PreferVendorRef(vendorRef, tbBaseSAP.Text);
            _isSelectingTicketSuggestion = true;
            tbTiketNum.Text = row["U_SOL_TIKET_WB"]?.ToString() ?? "";
            _isSelectingTicketSuggestion = false;
            this.noKontrak = (row["U_SOL_NO_KONTRAK"] != null && row["U_SOL_NO_KONTRAK"] != DBNull.Value)
                ? row["U_SOL_NO_KONTRAK"].ToString()
                : "";

            string tiketNo = row["U_SOL_TIKET_WB"]?.ToString() ?? "";
            if (!_isSplitKontrakTicket && !string.IsNullOrEmpty(tiketNo))
            {
                CheckIfSplitTicket(tiketNo);
            }
            lblValStatusWB.Text = row["U_SOL_STATUS"]?.ToString() ?? "";
            tbNopol.Text = row["U_SOL_NOPOL"]?.ToString() ?? "";
            tbNamaSupir.Text = row["U_SOL_NAMA_SUPIR"]?.ToString() ?? "";
            tbSIMSupir.Text = row["U_SOL_NO_SIM_SUPIR"]?.ToString() ?? "";
            tbJnsTruck.Text = row["U_SOL_JENIS_TRUCK"]?.ToString() ?? "";
            tbBrutoKebun.Text = DataConversionHelper.SafeToDouble(row["U_SOL_BRUTO_KEBUN"]).ToString("0.##");
            tbTaraKebun.Text = DataConversionHelper.SafeToDouble(row["U_SOL_TARA_KEBUN"]).ToString("0.##");
            tbNettoKebun.Text = DataConversionHelper.SafeToDouble(row["U_SOL_NETTO_KEBUN"]).ToString("0.##");
            tbFFAKebun.Text = row["U_SOL_FFA_KEBUN"]?.ToString() ?? "";
            tbMOISTKebun.Text = row["U_SOL_MOIST_KEBUN"]?.ToString() ?? "";
            tbFFA.Text = row["U_SOL_LAB_FFA"]?.ToString() ?? "";
            tbMOIST.Text = row["U_SOL_LAB_MOIST"]?.ToString() ?? "";
            tbDOBI.Text = row["U_SOL_LAB_DOBI"]?.ToString() ?? "";
            tbIMP.Text = row["U_SOL_LAB_IMP"]?.ToString() ?? "";
            tbCAROTINE.Text = row["U_SOL_LAB_CAROTINE"]?.ToString() ?? "";
            tbSealNo.Text = row["U_SOL_SEAL_NO"]?.ToString() ?? "";
            tbDONo.Text = row["U_SOL_DO_NO"]?.ToString() ?? "";
            tbNote.Text = row["U_SOL_NOTE"]?.ToString() ?? "";
            tbNoDokPabean.Text = row["U_SOL_NO_DOKUMEN_PABEAN"]?.ToString() ?? "";
            tbNomorSPB.Text = row["U_SOL_NOMOR_SPB"]?.ToString() ?? "";
            tbNomorAju.Text = row["U_SOL_NOMOR_AJU"]?.ToString() ?? "";
            tbContNo1.Text = row["U_SOL_CONTAINER_1_NO"]?.ToString() ?? "";
            tbContNo2.Text = row["U_SOL_CONTAINER_2_NO"]?.ToString() ?? "";

            tbQtyMasuk.Text = DataConversionHelper.SafeToDouble(row["U_SOL_BERAT_MASUK"]).ToString("0.##");
            tbQtyKeluar.Text = DataConversionHelper.SafeToDouble(row["U_SOL_BERAT_KELUAR"]).ToString("0.##");
            tbQtyNetto.Text = DataConversionHelper.SafeToDouble(row["U_SOL_BERAT_NETTO"]).ToString("0.##");
            lbValDocEntSAP.Text = docEntry.ToString();
            lblValLineNumSAP.Text = linenumSAP.ToString();
            valTanggalMasuk.Text = row["U_SOL_TANGGAL_MASUK"]?.ToString() ?? "";
            valWaktuMasuk.Text = row["U_SOL_WAKTU_MASUK"]?.ToString() ?? "";
            tbTransporterInt.Text = row["U_SOL_TRANSPORTER_INT"]?.ToString() ?? "";
            tbTransporterEks.Text = row["U_SOL_TRANSPORTER_EKS"]?.ToString() ?? "";

            FillPabeanFromSapIfMissing(docTypeVal, docEntry);
            ShowSplitKebunTotals();
        }

        private void ShowSplitKebunTotals()
        {
            if (!_isSplitKontrakTicket || _splitRows.Count < 2) return;

            var total = SplitTicketRowHelper.CombineKebun(
                _splitRows.Select(r => DataConversionHelper.SafeToDouble(r["U_SOL_NETTO_KEBUN"])),
                DataConversionHelper.SafeToDouble(_splitRows[0]["U_SOL_TARA_KEBUN"]));

            tbBrutoKebun.Text = total.Bruto.ToString("0.##");
            tbTaraKebun.Text = total.Tara.ToString("0.##");
            tbNettoKebun.Text = total.Netto.ToString("0.##");
        }

        private bool HasTanggalPabean()
        {
            return dtTglDokPabean.Checked && dtTglDokPabean.CustomFormat != " ";
        }

        private void SetTanggalPabean(DateTime tanggal)
        {
            dtTglDokPabean.Value = tanggal;
            dtTglDokPabean.Checked = true;
            dtTglDokPabean.CustomFormat = null;
        }

        private void ClearTanggalPabean()
        {
            dtTglDokPabean.Checked = false;
            dtTglDokPabean.CustomFormat = " ";
        }

        private void FillPabeanFromSapIfMissing(string docType, int docEntry)
        {
            bool lengkap = !string.IsNullOrWhiteSpace(cmbKodeDokPabean.Text) && !string.IsNullOrWhiteSpace(tbNoDokPabean.Text) && HasTanggalPabean();
            if (lengkap || docEntry <= 0) return;

            var pabean = _pabeanService.GetPabeanData(docType, docEntry);
            if (pabean == null) return;

            if (string.IsNullOrWhiteSpace(cmbKodeDokPabean.Text) && !string.IsNullOrWhiteSpace(pabean.KodeDokumen)) cmbKodeDokPabean.Text = pabean.KodeDokumen;
            if (string.IsNullOrWhiteSpace(tbNoDokPabean.Text) && !string.IsNullOrWhiteSpace(pabean.NomorDokumen)) tbNoDokPabean.Text = pabean.NomorDokumen;
            if (!HasTanggalPabean() && pabean.TanggalDokumen.HasValue) SetTanggalPabean(pabean.TanggalDokumen.Value);
            if (string.IsNullOrWhiteSpace(tbNomorSPB.Text) && !string.IsNullOrWhiteSpace(pabean.NomorSPB)) tbNomorSPB.Text = pabean.NomorSPB;
            if (string.IsNullOrWhiteSpace(tbNomorAju.Text) && !string.IsNullOrWhiteSpace(pabean.NomorAju)) tbNomorAju.Text = pabean.NomorAju;
        }

        private bool ValidasiDokumenPabean()
        {
            var val = _pabeanService.Validate(
                cmbKodeDokPabean.Text,
                tbNoDokPabean.Text,
                HasTanggalPabean(),
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

            string selDocType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text);
            if (!ValidasiBeratTimbang(selDocType)) return;

            DialogResult result = MessageBox.Show("Pastikan semua data sudah benar. Lanjut simpan data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            string tiketNum = tbTiketNum.Text;
            try
            {
                bool synced;
                if (selDocType == "PO")
                {
                    if (!RefreshSplitStateFromDatabase(tiketNum)) return;
                    if (!EnsureNoPostedDocument("OPDN", "GRPO", tiketNum)) return;

                    synced = _isSplitKontrakTicket ? SyncSplitGRPO() : SyncGRPO();
                }
                else
                {
                    if (!EnsureNoPostedDocument("ODLN", "Delivery Order", tiketNum)) return;
                    int records = _hanaRepo.SearchTicketByTiketNum(tiketNum).Rows.Count;
                    if (records > 1)
                    {
                        MessageBox.Show(
                            $"Tiket SO {tiketNum} tersimpan sebagai split kontrak ({records} record).\n" +
                            "Split kontrak untuk SO belum tersedia, Delivery Order tidak dibuat. Hubungi admin.",
                            "Split Kontrak SO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    synced = SyncDO();
                }

                // Gagal / dibatalkan: jangan cetak tiket dan jangan kosongkan form
                if (!synced) return;

                PrintWeighbridgeTicket(tiketNum);

                ClearAllField();
                DisableFieldWB();
                tbBaseDoc.Focus();
                btnCariBaseDoc.Visible = true;

                tbTiketNum.Enabled = true;
                tbQtyKeluar.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                ClearAllField();
                DisableFieldWB();
                btnCariBaseDoc.Visible = true;
                lblStatusWB.Visible = false;
                lblValStatusWB.Visible = false;
                tbTiketNum.Enabled = true;

                if (lstTicketSuggestions != null)
                {
                    lstTicketSuggestions.Visible = false;
                    lstTicketSuggestions.Items.Clear();
                }
                LoadPendingTickets(silent: true);
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
                cbTKManual.Enabled = AppFeatures.AllowManualWeighing;
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
                btnWBSave.Enabled = true;
                tbTransporterInt.Enabled = true;
                tbTransporterEks.Enabled = true;
                cmbTangkiNo.Enabled = true;
            }

            // Tiket split: kebun berisi total K1 + K2 (tampilan saja), perubahan di sini tidak dipakai GRPO split
            if (_isSplitKontrakTicket)
            {
                tbBrutoKebun.Enabled = false;
                tbTaraKebun.Enabled = false;
                tbNettoKebun.Enabled = false;
            }
        }

        private void DisableFieldWB()
        {
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
            cbTKManual.Checked = false;
            cbTKManual.Enabled = false;
            btnTimbangKeluar.Enabled = false;
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
        }

        private void ClearAllField()
        {
            lbValDocEntSAP.Text = string.Empty;
            lblValLineNumSAP.Text = string.Empty;
            tbBaseSAP.Text = string.Empty;
            valTanggalMasuk.Text = string.Empty;
            valWaktuMasuk.Text = string.Empty;
            cbDocType.SelectedIndex = -1;
            tbBaseDoc.Text = string.Empty;
            tbTiketNum.Text = string.Empty;
            tbNopol.Text = string.Empty;
            tbNamaSupir.Text = string.Empty;
            tbSIMSupir.Text = string.Empty;
            tbJnsTruck.Text = string.Empty;
            tbBrutoKebun.Text = string.Empty;
            tbTaraKebun.Text = string.Empty;
            tbNettoKebun.Text = string.Empty;
            tbFFAKebun.Text = string.Empty;
            tbMOISTKebun.Text = string.Empty;
            tbFFA.Text = string.Empty;
            tbMOIST.Text = string.Empty;
            tbDOBI.Text = string.Empty;
            tbIMP.Text = string.Empty;
            tbCAROTINE.Text = string.Empty;
            tbQtyMasuk.Text = "0";
            tbQtyKeluar.Text = "0";
            tbQtyNetto.Text = "0";
            lblValStatusWB.Text = string.Empty;
            tbNoDokPabean.Text = string.Empty;
            cmbKodeDokPabean.Text = string.Empty;
            tbNomorSPB.Text = string.Empty;
            tbNomorAju.Text = string.Empty;
            tbContNo1.Text = string.Empty;
            tbContNo2.Text = string.Empty;
            tbSealNo.Text = string.Empty;
            tbDONo.Text = string.Empty;
            tbNote.Text = string.Empty;
            tbCommodity.Text = string.Empty;
            dtTglDokPabean.Value = DateTime.Now;
            ClearTanggalPabean();
            // cbTKManual hidden - not cleared
            tbTransporterInt.Text = string.Empty;
            tbTransporterEks.Text = string.Empty;
            cmbTangkiNo.Text = string.Empty;
            ClearSplitRows();
            this.noKontrak = string.Empty;

            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.Visible = false;
                lstTicketSuggestions.Items.Clear();
            }
        }

        private WeighOutData BuildWeighOutData(DataRow row = null, double beratNetto = 0)
        {
            string code = "";
            if (row != null)
            {
                if (row.Table.Columns.Contains("Code")) code = row["Code"]?.ToString() ?? "";
                else if (row.Table.Columns.Contains("CODE")) code = row["CODE"]?.ToString() ?? "";
            }
            if (string.IsNullOrEmpty(code)) code = _hanaRepo.GetWBCodeByTiket(tbTiketNum.Text);

            double resolvedNetto = beratNetto;
            if (resolvedNetto <= 0 && row == null)
            {
                resolvedNetto = DataConversionHelper.SafeToDouble(tbQtyNetto.Text);
            }

            return new WeighOutData
            {
                TiketNum = tbTiketNum.Text ?? "",
                Code = code,
                DocType = cbDocType.Text ?? "PO",
                DocEntry = row != null ? DataConversionHelper.SafeToInt(row["U_SOL_DOC_ENTRY_SAP"]) : DataConversionHelper.SafeToInt(lbValDocEntSAP.Text),
                LineNum = row != null ? DataConversionHelper.SafeToInt(row["U_SOL_LINENUM_SAP"]) : DataConversionHelper.SafeToInt(lblValLineNumSAP.Text),
                DocNum = row != null ? (row["U_SOL_DOCNUM_SAP"]?.ToString() ?? "") : (tbBaseSAP.Text ?? ""),
                NoKontrak = row != null ? (row["U_SOL_NO_KONTRAK"]?.ToString() ?? "") : (noKontrak ?? ""),
                TangkiNo = cmbTangkiNo.SelectedIndex != -1 ? cmbTangkiNo.Text : "",
                BeratMasuk = DataConversionHelper.SafeToDouble(tbQtyMasuk.Text),
                BeratKeluar = DataConversionHelper.SafeToDouble(tbQtyKeluar.Text),
                BeratNetto = resolvedNetto,
                Status = "SYNCSAP",
                WeighoutFlag = "Y",
                // Teks yang sama dengan yang divalidasi; nilai dari SAP bisa tidak persis sama dengan item daftar
                KodeDokumenPabean = cmbKodeDokPabean.Text?.Trim() ?? "",
                NoDokumenPabean = tbNoDokPabean.Text ?? "",
                TanggalDokumenPabean = HasTanggalPabean() ? (DateTime?)dtTglDokPabean.Value : null,
                NomorSpb = tbNomorSPB.Text ?? "",
                NomorAju = tbNomorAju.Text ?? "",
                NamaSupir = tbNamaSupir.Text ?? "",
                JenisTruck = tbJnsTruck.Text ?? "",
                Nopol = tbNopol.Text ?? "",
                TransporterInt = tbTransporterInt.Text ?? "",
                TransporterEks = tbTransporterEks.Text ?? "",
                BrutoKebun = NormalizeKebunValue(row != null ? row["U_SOL_BRUTO_KEBUN"]?.ToString() : tbBrutoKebun.Text),
                TaraKebun = NormalizeKebunValue(row != null ? row["U_SOL_TARA_KEBUN"]?.ToString() : tbTaraKebun.Text),
                NettoKebun = NormalizeKebunValue(row != null ? row["U_SOL_NETTO_KEBUN"]?.ToString() : tbNettoKebun.Text),
                FfaKebun = row != null ? (row["U_SOL_FFA_KEBUN"]?.ToString() ?? "") : (tbFFAKebun.Text ?? ""),
                MoistKebun = row != null ? (row["U_SOL_MOIST_KEBUN"]?.ToString() ?? "") : (tbMOISTKebun.Text ?? ""),
                LabFfa = !string.IsNullOrWhiteSpace(tbFFA.Text) ? tbFFA.Text : (row != null ? (row["U_SOL_LAB_FFA"]?.ToString() ?? "") : ""),
                LabMoist = !string.IsNullOrWhiteSpace(tbMOIST.Text) ? tbMOIST.Text : (row != null ? (row["U_SOL_LAB_MOIST"]?.ToString() ?? "") : ""),
                LabImp = !string.IsNullOrWhiteSpace(tbIMP.Text) ? tbIMP.Text : (row != null ? (row["U_SOL_LAB_IMP"]?.ToString() ?? "") : ""),
                LabDobi = !string.IsNullOrWhiteSpace(tbDOBI.Text) ? tbDOBI.Text : (row != null ? (row["U_SOL_LAB_DOBI"]?.ToString() ?? "") : ""),
                LabCarotine = !string.IsNullOrWhiteSpace(tbCAROTINE.Text) ? tbCAROTINE.Text : (row != null ? (row["U_SOL_LAB_CAROTINE"]?.ToString() ?? "") : ""),
                SealNo = !string.IsNullOrWhiteSpace(tbSealNo.Text) ? tbSealNo.Text : (row != null ? (row["U_SOL_SEAL_NO"]?.ToString() ?? "") : ""),
                DoNo = !string.IsNullOrWhiteSpace(tbDONo.Text) ? tbDONo.Text : (row != null ? (row["U_SOL_DO_NO"]?.ToString() ?? "") : ""),
                Note = !string.IsNullOrWhiteSpace(tbNote.Text) ? tbNote.Text : (row != null ? (row["U_SOL_NOTE"]?.ToString() ?? "") : ""),
                Container1No = !string.IsNullOrWhiteSpace(tbContNo1.Text) ? tbContNo1.Text : (row != null ? (row["U_SOL_CONTAINER_1_NO"]?.ToString() ?? "") : ""),
                Container2No = !string.IsNullOrWhiteSpace(tbContNo2.Text) ? tbContNo2.Text : (row != null ? (row["U_SOL_CONTAINER_2_NO"]?.ToString() ?? "") : "")
            };
        }

        private bool ValidasiBeratTimbang(string docType)
        {
            double qtyMasuk = DataConversionHelper.SafeToDouble(tbQtyMasuk.Text);
            double qtyKeluar = DataConversionHelper.SafeToDouble(tbQtyKeluar.Text);

            if (qtyMasuk <= 0)
            {
                MessageBox.Show("Berat masuk tidak valid (0 atau kosong).", "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (qtyKeluar <= 0)
            {
                MessageBox.Show(
                    "Berat keluar belum ditimbang (0 atau kosong).\n" +
                    "Lakukan Timbang Keluar terlebih dahulu sebelum menyimpan.",
                    "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool isPO = docType == "PO";
            if (isPO && qtyKeluar >= qtyMasuk)
            {
                MessageBox.Show(
                    $"Berat keluar ({DocumentLookupItem.FormatKg(qtyKeluar)} kg) harus lebih kecil dari berat masuk ({DocumentLookupItem.FormatKg(qtyMasuk)} kg) untuk dokumen PO.",
                    "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!isPO && qtyKeluar <= qtyMasuk)
            {
                MessageBox.Show(
                    $"Berat keluar ({DocumentLookupItem.FormatKg(qtyKeluar)} kg) harus lebih besar dari berat masuk ({DocumentLookupItem.FormatKg(qtyMasuk)} kg) untuk dokumen SO.",
                    "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool RefreshSplitStateFromDatabase(string tiketNum)
        {
            DataTable dt;
            try
            {
                dt = _hanaRepo.SearchTicketByTiketNum(tiketNum);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal membaca data tiket dari SAP, proses dibatalkan:\n" + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (dt == null || dt.Rows.Count == 0)
            {
                MessageBox.Show($"Data tiket {tiketNum} tidak ditemukan di SAP. Proses dibatalkan.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!ValidateAllRowsWeighIn(dt, tiketNum)) return false;

            if (dt.Rows.Count >= 2)
            {
                if (!_isSplitKontrakTicket)
                {
                    MessageBox.Show(
                        $"Tiket {tiketNum} terdeteksi sebagai SPLIT KONTRAK ({dt.Rows.Count} record).\n" +
                        $"GRPO akan dibagi ke {dt.Rows.Count} kontrak.",
                        "Split Kontrak Terdeteksi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                SetSplitRows(dt);
            }
            else
            {
                ClearSplitRows();
            }
            return true;
        }

        private bool EnsureNoPostedDocument(string tableName, string docLabel, string tiketNum)
        {
            string existing = _hanaRepo.GetPostedDocNumsForTicket(tableName, tiketNum);
            if (string.IsNullOrEmpty(existing)) return true;

            MessageBox.Show(
                $"Tiket {tiketNum} sudah memiliki {docLabel} di SAP (No. {existing}), tetapi status Weighbridge-nya masih WEIGHIN.\n\n" +
                $"Proses dibatalkan agar {docLabel} tidak terbentuk ganda. Hubungi admin SAP untuk memperbaiki status tiket ini.",
                $"{docLabel} Sudah Ada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool ValidateAllRowsWeighIn(DataTable dt, string tiketNum)
        {
            if (!dt.Columns.Contains("U_SOL_STATUS")) return true;

            foreach (DataRow r in dt.Rows)
            {
                string status = r["U_SOL_STATUS"]?.ToString()?.Trim() ?? "";
                if (!string.Equals(status, "WEIGHIN", StringComparison.OrdinalIgnoreCase))
                {
                    string doc = dt.Columns.Contains("U_SOL_DOCNUM_SAP") ? r["U_SOL_DOCNUM_SAP"]?.ToString() ?? "" : "";
                    MessageBox.Show(
                        $"Tiket #{tiketNum} memiliki record (Doc {doc}) yang sudah berstatus {status}.\n" +
                        "Tiket ini tidak dapat diproses lagi untuk timbang keluar agar GRPO tidak terbentuk ganda. Hubungi admin SAP.",
                        "Tiket Sudah Diproses", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            return true;
        }

        private bool CheckAndFixKebunSwap()
        {
            double taraKebun = DataConversionHelper.ParseWeightInput(tbTaraKebun.Text);
            double nettoKebun = DataConversionHelper.ParseWeightInput(tbNettoKebun.Text);
            double taraWB = Math.Min(DataConversionHelper.SafeToDouble(tbQtyMasuk.Text), DataConversionHelper.SafeToDouble(tbQtyKeluar.Text));

            if (!WeighbridgeCalculator.IsTaraNettoKebunSwapped(taraKebun, nettoKebun, taraWB))
                return true;

            var res = MessageBox.Show(
                "Data kebun kemungkinan TERTUKAR antara Tara dan Netto:\n\n" +
                $"  Tara Kebun  : {DocumentLookupItem.FormatKg(taraKebun)} kg\n" +
                $"  Netto Kebun : {DocumentLookupItem.FormatKg(nettoKebun)} kg\n" +
                $"  Tara WB     : {DocumentLookupItem.FormatKg(taraWB)} kg (berat truk kosong timbangan perusahaan)\n\n" +
                "Netto Kebun lebih mendekati Tara WB, sehingga kemungkinan besar angka tertukar saat timbang masuk.\n\n" +
                "Tukar otomatis menjadi:\n" +
                $"  Tara Kebun  : {DocumentLookupItem.FormatKg(nettoKebun)} kg\n" +
                $"  Netto Kebun : {DocumentLookupItem.FormatKg(taraKebun)} kg ?\n\n" +
                "Yes = tukar lalu lanjut, No = batal (periksa SPB kebun).",
                "Data Kebun Tertukar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (res != DialogResult.Yes) return false;

            tbTaraKebun.Text = nettoKebun.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            tbNettoKebun.Text = taraKebun.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Truk non-split selalu di-GRPO penuh ke PO-nya; surplus di atas sisa qty PO = kelebihan terima (seperti aplikasi lama).
        /// Hanya untuk informasi di pesan sukses; kosong bila muat atau sisa PO tidak terbaca.
        /// </summary>
        private string GetKelebihanTerimaInfo(WeighOutData data)
        {
            try
            {
                var fresh = _hanaRepo.GetFreshQuota("PO", data.DocEntry, data.LineNum);
                if (fresh.totalQty <= 0 || data.BeratNetto <= fresh.openQty + 0.001) return "";
                return $"\n\nCatatan: netto {DocumentLookupItem.FormatKg(data.BeratNetto)} kg melebihi sisa qty PO {data.DocNum} " +
                       $"({DocumentLookupItem.FormatKg(Math.Max(0, fresh.openQty))} kg), PO kelebihan terima " +
                       $"{DocumentLookupItem.FormatKg(data.BeratNetto - Math.Max(0, fresh.openQty))} kg.";
            }
            catch
            {
                return "";
            }
        }

        private void ShowSyncResult(bool ok, string error, string successMsg)
        {
            if (!ok)
            {
                MessageBox.Show("Error : " + error, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(successMsg + "\n\nPERINGATAN:\n" + error, "Peringatan - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(successMsg, "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private bool SyncGRPO()
        {
            if (!CheckAndFixKebunSwap()) return false;

            var data = BuildWeighOutData();
            if (data.BeratNetto <= 0)
            {
                MessageBox.Show(
                    "Netto WB tidak valid (0 atau kosong).\n" +
                    "Pastikan berat masuk dan berat keluar sudah diisi dari timbangan WB perusahaan.",
                    "Validasi Netto WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string kelebihanInfo = GetKelebihanTerimaInfo(data);
            bool ok = _weighOutService.SyncGRPO(data, out int createdDocEntry, out string error);
            ShowSyncResult(ok, error, "Data Synced to SAP" + kelebihanInfo);
            return ok;
        }

        private bool SyncDO()
        {
            int docEntry = DataConversionHelper.SafeToInt(lbValDocEntSAP.Text);
            int linenumSAP = DataConversionHelper.SafeToInt(lblValLineNumSAP.Text);
            var (itemCode, whsCode) = _hanaRepo.GetItemAndWhsCodeFromSO(docEntry, linenumSAP);

            var data = BuildWeighOutData();
            if (data.BeratNetto <= 0)
            {
                MessageBox.Show("Netto WB tidak valid (0 atau kosong).", "Validasi Netto WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool ok = _weighOutService.SyncDeliveryOrder(data, itemCode, whsCode, out int createdDocEntry, out string error);
            ShowSyncResult(ok, error, "Data Synced to SAP");
            return ok;
        }

        private static string NormalizeKebunValue(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            double v = DataConversionHelper.SafeToDouble(raw.Trim());
            return v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
