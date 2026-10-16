using System;
using System.Data;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class timbangKeluarUpdateGRPODOForm : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;
        private readonly IPrintService _printService;
        private readonly SerialPortService _serialPortService;

        private WeighStatusForm _statusForm;
        public string timbangType = "A";
        private string noKontrak = "";

        public timbangKeluarUpdateGRPODOForm() : this(new HanaRepository(), new SapRepository(), new PrintService(), new SerialPortService())
        {
        }

        public timbangKeluarUpdateGRPODOForm(IHanaRepository hanaRepo, ISapRepository sapRepo, IPrintService printService, SerialPortService serialPortService)
        {
            _hanaRepo = hanaRepo ?? new HanaRepository();
            _sapRepo = sapRepo ?? new SapRepository();
            _printService = printService ?? new PrintService(_hanaRepo);
            _serialPortService = serialPortService ?? new SerialPortService();

            InitializeComponent();
        }

        private void timbangKeluarUpdateGRPODOForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;
            cbTKManual.Visible = false;
            cbTKManual.Enabled = false;
            cbTKManual.Checked = false;
            timbangType = "A";
            ResponsiveLayoutHelper.Attach(this);
            WatermarkHelper.Apply(this);
        }

        private void InitializeSerialPort()
        {
            _serialPortService.OnWeightReceived += weight =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(() =>
                {
                    tbQtyKeluar.Text = weight;
                    _statusForm?.ShowSuccess(weight);
                }));
            };

            _serialPortService.OnError += error =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(() =>
                {
                    _statusForm?.Close();
                    MessageBox.Show($"Read error: {error}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            };

            _serialPortService.Initialize();
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
            // Manual checkbox dinonaktifkan
        }

        private void btnTimbangKeluar_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPortService.StartReading();
                ShowReadingDialog("Timbang Keluar");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnTimbangKeluar_Click_1(object sender, EventArgs e)
        {
            btnTimbangKeluar_Click(sender, e);
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
            // Berat keluar 0 (belum ditimbang) tidak boleh menghasilkan netto = berat masuk
            double qtyNetto = qtyKeluar > 0 ? WeighbridgeCalculator.CalculateNetto(qtyMasuk, qtyKeluar) : 0;
            tbQtyNetto.Text = qtyNetto.ToString("0.##");
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

            string tiketNum = tbTiketNum.Text.Trim();
            int docEntryDOSAP = int.TryParse(lblValDocEntDOSAP.Text, out int de) ? de : 0;
            int lineNumDOSAP = int.TryParse(lblValLineNumDOSAP.Text, out int ln) ? ln : 0;
            double qtyKeluar = DataConversionHelper.SafeToDouble(tbQtyKeluar.Text);
            double qtyNetto = qtyKeluar > 0 ? DataConversionHelper.SafeToDouble(tbQtyNetto.Text) : 0;
            string docType = cbDocType.Text.Trim();

            if (qtyKeluar <= 0 || qtyNetto <= 0)
            {
                MessageBox.Show(
                    "Berat keluar belum ditimbang (0 atau kosong).\nLakukan Timbang Keluar terlebih dahulu sebelum menyimpan.",
                    "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_hanaRepo.GetWBCodesByTiket(tiketNum).Count > 1)
            {
                MessageBox.Show(
                    $"Tiket {tiketNum} adalah tiket SPLIT KONTRAK (lebih dari 1 record).\n" +
                    "Tiket split tidak bisa diproses di form Update karena netto harus dibagi ke K1 dan K2. Gunakan form Timbang Keluar.",
                    "Tiket Split Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show("Pastikan semua data sudah benar. Lanjut simpan data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            bool success;
            string sapError;

            if (docType == "GRPO")
            {
                success = _sapRepo.UpdateGrpoLineWeight(docEntryDOSAP, lineNumDOSAP, qtyNetto, out sapError);
            }
            else
            {
                success = _sapRepo.UpdateDoLineWeight(docEntryDOSAP, lineNumDOSAP, qtyNetto, out sapError);
            }

            if (!success)
            {
                MessageBox.Show("Error : " + sapError, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string code = _hanaRepo.GetWBCodeByTiket(tiketNum);
            if (!string.IsNullOrEmpty(code))
            {
                if (!_sapRepo.UpdateWeighOutUDO(code, qtyKeluar, qtyNetto, "SYNCSAP", "Y", "", out string udoError))
                {
                    MessageBox.Show("Warning updating UDO status: " + udoError, "Warning - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show("Data Synced to SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (!_printService.PrintTicket(tiketNum, TicketPrintType.Keluar, out string printError))
            {
                MessageBox.Show($"Failed to print ticket:\n{printError}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ClearAllField();
            DisableFieldWB();
            tbBaseDoc.Focus();
            btnCariBaseDoc.Visible = true;
            btnRefreshData.Visible = false;
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

            if (!string.IsNullOrEmpty(tbBaseDoc.Text))
            {
                if (string.IsNullOrEmpty(cbDocType.Text))
                {
                    MessageBox.Show("Pilih Document Type terlebih dahulu", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string docType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "" : cbDocType.Text);
                string docNum = tbBaseDoc.Text.Trim();

                if (string.IsNullOrEmpty(docType) || string.IsNullOrEmpty(docNum))
                {
                    MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DataTable result = _hanaRepo.SearchBaseDocJoinWbData(docType, docNum);
                if (result == null || result.Rows.Count == 0)
                {
                    MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DisableFieldWB();
                    ClearAllField();
                    return;
                }

                if (result.Rows.Count == 1)
                {
                    MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    fillFormFromDataRow(result.Rows[0]);
                }
                else
                {
                    MessageBox.Show($"Document SAP ditemukan. Total {result.Rows.Count} tiket. Pilih salah satu tiket.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    using (SelectTicketForm selectTicketForm = new SelectTicketForm(result))
                    {
                        if (selectTicketForm.ShowDialog(this) == DialogResult.OK && selectTicketForm.SelectedTicket != null)
                        {
                            fillFormFromDataRow(selectTicketForm.SelectedTicket);
                        }
                    }
                }
            }
            else
            {
                string noTiket = tbTiketNum.Text.Trim();
                DataTable result = _hanaRepo.SearchTicketByTiketNum(noTiket);
                if (result == null || result.Rows.Count == 0)
                {
                    MessageBox.Show("Nomor Tiket tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DisableFieldWB();
                    ClearAllField();
                    return;
                }

                MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fillFormFromDataRow(result.Rows[0]);
            }

            tbTransporterInt.Enabled = false;
        }

        private void fillFormFromDataRow(DataRow row)
        {
            string GetVal(string col) => row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col].ToString() : "";

            int docEntry = DataConversionHelper.SafeToInt(row["U_SOL_DOC_ENTRY_SAP"]);
            noKontrak = GetVal("U_SOL_NO_KONTRAK");
            int linenumSAP = DataConversionHelper.SafeToInt(row["U_SOL_LINENUM_SAP"]);

            if (row.Table.Columns.Contains("U_SOL_TANGGAL_DOKUMEN") && row["U_SOL_TANGGAL_DOKUMEN"] != DBNull.Value)
            {
                dtTglDokPabean.Value = Convert.ToDateTime(row["U_SOL_TANGGAL_DOKUMEN"]);
            }
            else
            {
                dtTglDokPabean.CustomFormat = " ";
            }

            cbDocType.Text = GetVal("U_SOL_DOC_TYPE");
            string vendorRef = GetVal("U_SOL_VENDOR_REF");
            string docNumSap = GetVal("U_SOL_DOCNUM_SAP");
            tbBaseDoc.Text = !string.IsNullOrEmpty(vendorRef) ? vendorRef : docNumSap;
            tbTiketNum.Text = GetVal("U_SOL_TIKET_WB");
            lblValStatusWB.Text = GetVal("U_SOL_STATUS");

            tbNopol.Text = GetVal("U_SOL_NOPOL");
            tbNamaSupir.Text = GetVal("U_SOL_NAMA_SUPIR");
            tbSIMSupir.Text = GetVal("U_SOL_NO_SIM_SUPIR");
            tbJnsTruck.Text = GetVal("U_SOL_JENIS_TRUCK");
            tbCommodity.Text = GetVal("U_SOL_COMMODITY");
            tbSealNo.Text = GetVal("U_SOL_SEAL_NO");
            tbDONo.Text = GetVal("U_SOL_DO_NO");
            tbNote.Text = GetVal("U_SOL_NOTE");
            tbKodeDokPabean.Text = GetVal("U_SOL_KODE_DOKUMEN");
            tbNoDokPabean.Text = GetVal("U_SOL_NO_DOKUMEN_PABEAN");
            tbNomorSPB.Text = GetVal("U_SOL_NOMOR_SPB");
            tbNomorAju.Text = GetVal("U_SOL_NOMOR_AJU");
            tbContNo1.Text = GetVal("U_SOL_CONTAINER_1_NO");
            tbContNo2.Text = GetVal("U_SOL_CONTAINER_2_NO");

            tbQtyMasuk.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_MASUK") ? row["U_SOL_BERAT_MASUK"] : null).ToString("0.##");
            tbQtyKeluar.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_KELUAR") ? row["U_SOL_BERAT_KELUAR"] : null).ToString("0.##");
            tbQtyNetto.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_NETTO") ? row["U_SOL_BERAT_NETTO"] : null).ToString("0.##");

            lblValDocEntDOSAP.Text = docEntry.ToString();
            lblValLineNumDOSAP.Text = linenumSAP.ToString();
            valTanggalMasuk.Text = GetVal("U_SOL_TANGGAL_MASUK");
            valWaktuMasuk.Text = GetVal("U_SOL_WAKTU_MASUK");
            tbTransporterInt.Text = GetVal("U_SOL_TRANSPORTER_INT");
            tbTransporterEks.Text = GetVal("U_SOL_TRANSPORTER_EKS");

            EnableFieldWB();
            btnCariBaseDoc.Visible = false;
            btnRefreshData.Visible = true;
            lblStatusWB.Visible = true;
            lblValStatusWB.Visible = true;
            tbTiketNum.Enabled = false;
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
            // cbTKManual hidden - not enabled
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
            btnWBSave.Enabled = true;
            tbTransporterInt.Enabled = true;
            tbTransporterEks.Enabled = true;
        }

        private void DisableFieldWB()
        {
            tbNopol.Enabled = false;
            tbNamaSupir.Enabled = false;
            tbSIMSupir.Enabled = false;
            tbJnsTruck.Enabled = false;
            // cbTKManual hidden - not disabled
            btnTimbangKeluar.Enabled = false;
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
        }

        private void ClearAllField()
        {
            cbDocType.SelectedIndex = -1;
            tbBaseDoc.Text = string.Empty;
            tbTiketNum.Text = string.Empty;
            tbNopol.Text = string.Empty;
            tbNamaSupir.Text = string.Empty;
            tbSIMSupir.Text = string.Empty;
            tbJnsTruck.Text = string.Empty;
            tbQtyMasuk.Text = "0";
            tbQtyKeluar.Text = "0";
            tbQtyNetto.Text = "0";
            tbNoDokPabean.Text = string.Empty;
            tbKodeDokPabean.Text = string.Empty;
            tbNomorSPB.Text = string.Empty;
            tbNomorAju.Text = string.Empty;
            tbContNo1.Text = string.Empty;
            tbContNo2.Text = string.Empty;
            tbSealNo.Text = string.Empty;
            tbDONo.Text = string.Empty;
            tbNote.Text = string.Empty;
            tbCommodity.Text = string.Empty;
            dtTglDokPabean.Checked = false;
            dtTglDokPabean.Value = DateTime.Now;
            // cbTKManual hidden - not cleared
            tbTransporterInt.Text = string.Empty;
            tbTransporterEks.Text = string.Empty;
            noKontrak = "";
            lblValDocEntDOSAP.Text = string.Empty;
            lblValLineNumDOSAP.Text = string.Empty;
            tbTiketNum.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _serialPortService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
