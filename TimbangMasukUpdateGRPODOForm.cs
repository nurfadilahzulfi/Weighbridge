using System;
using System.Data;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class timbangMasukUpdateGRPODOForm : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;
        private readonly IPrintService _printService;
        private readonly SerialPortService _serialPortService;

        private WeighStatusForm _statusForm;
        public string timbangType = "A";
        private string noKontrak = "";

        public timbangMasukUpdateGRPODOForm() : this(new HanaRepository(), new SapRepository(), new PrintService(), new SerialPortService())
        {
        }

        public timbangMasukUpdateGRPODOForm(IHanaRepository hanaRepo, ISapRepository sapRepo, IPrintService printService, SerialPortService serialPortService)
        {
            _hanaRepo = hanaRepo ?? new HanaRepository();
            _sapRepo = sapRepo ?? new SapRepository();
            _printService = printService ?? new PrintService(_hanaRepo);
            _serialPortService = serialPortService ?? new SerialPortService();

            InitializeComponent();
        }

        private void UpdateDeliveryOrderForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataComboBox();
            runningTime.Start();
            lblUsername.Text = SessionUser.username;
            cbTransInt.Enabled = false;
            cbTMManual.Visible = false;
            cbTMManual.Enabled = false;
            cbTMManual.Checked = false;
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
                    tbQtyMasuk.Text = weight;
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

        private void runningTime_Tick(object sender, EventArgs e)
        {
            lbDateTime.Text = DateTime.Now.ToLongDateString() + " / " + DateTime.Now.ToLongTimeString();
        }

        private void ShowReadingDialog(string type)
        {
            _statusForm = new WeighStatusForm();
            _statusForm.ShowReading();
            _statusForm.Show(this);
        }

        private void chbManualMasuk_CheckedChanged(object sender, EventArgs e)
        {
            // Manual checkbox dinonaktifkan
        }

        private void tbQtyTimbangMasuk_KeyPress(object sender, KeyPressEventArgs e)
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

        private void btnTimbangMasuk_Click(object sender, EventArgs e)
        {
            try
            {
                _serialPortService.StartReading();
                ShowReadingDialog("Timbang Masuk");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCariBaseDoc_Click(object sender, EventArgs e)
        {
            LoadDataComboBox();

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

            string docType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "" : cbDocType.Text);
            string docNum = tbBaseDoc.Text.Trim();

            if (string.IsNullOrEmpty(docType) || string.IsNullOrEmpty(docNum))
            {
                MessageBox.Show("Document Type atau Document Number SAP tidak boleh kosong.", "Error - Weighbridge UI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataTable result = _hanaRepo.SearchBaseDocGrpoDo(docType, docNum);
            if (result == null || result.Rows.Count == 0)
            {
                MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableFieldWB();
                ClearAllField();
                return;
            }

            MessageBox.Show("Document SAP ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DataRow row = result.Rows[0];
            tbTiketNum.Text = _hanaRepo.GenerateTiketNumber();
            tbTiketNum.Enabled = false;

            if (row["TANGGALDOKUMEN"] == DBNull.Value)
            {
                dtTglDokPabean.CustomFormat = " ";
            }
            else
            {
                dtTglDokPabean.Value = Convert.ToDateTime(row["TANGGALDOKUMEN"]);
            }

            int docEntry = DataConversionHelper.SafeToInt(row["DOCENTRY"]);
            noKontrak = DataConversionHelper.NormalizeKontrak(row["KONTRAK"]);
            string itemCode = row["ITEMCODE"]?.ToString() ?? "";
            int linenumSAP = DataConversionHelper.SafeToInt(row["LINENUM"]);

            tbCommodity.Text = itemCode;
            tbKodeDokPabean.Text = row["KODEDOKUMEN"]?.ToString() ?? "";
            tbNoDokPabean.Text = row["NOMORDOKUMEN"]?.ToString() ?? "";
            tbNomorSPB.Text = row["NOMORSPB"]?.ToString() ?? "";
            tbNomorAju.Text = row["NOMORAJU"]?.ToString() ?? "";
            lblValDocEntDOSAP.Text = docEntry.ToString();
            lblValLineNumDOSAP.Text = linenumSAP.ToString();

            EnableFieldWB();
            btnCariBaseDoc.Visible = false;
            btnRefreshData.Visible = true;
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
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string wbCode = _hanaRepo.GetNextWBCode();
                string transporterIntCode = cbTransInt.SelectedValue != null ? cbTransInt.SelectedValue.ToString() : "";

                var weighInData = new WeighInData
                {
                    Code = wbCode,
                    DocType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text),
                    DocNum = tbBaseDoc.Text.Trim(),
                    DocEntry = int.TryParse(lblValDocEntDOSAP.Text, out int de) ? de : 0,
                    LineNum = int.TryParse(lblValLineNumDOSAP.Text, out int ln) ? ln : 0,
                    TiketNum = tbTiketNum.Text.Trim(),
                    Commodity = tbCommodity.Text.Trim(),
                    NoKontrak = noKontrak,
                    Nopol = tbNopol.Text.Trim(),
                    NamaSupir = tbNamaSupir.Text.Trim(),
                    NoSimSupir = tbSIMSupir.Text.Trim(),
                    JenisTruck = tbJnsTruck.Text.Trim(),
                    BeratMasuk = DataConversionHelper.SafeToDouble(tbQtyMasuk.Text),
                    KodeDokumenPabean = tbKodeDokPabean.Text.Trim(),
                    NoDokumenPabean = tbNoDokPabean.Text.Trim(),
                    TanggalDokumenPabean = dtTglDokPabean.CustomFormat != " " ? dtTglDokPabean.Value : (DateTime?)null,
                    NomorSpb = tbNomorSPB.Text.Trim(),
                    NomorAju = tbNomorAju.Text.Trim(),
                    SealNo = tbSealNo.Text.Trim(),
                    DoNo = tbDONo.Text.Trim(),
                    Container1No = tbContNo1.Text.Trim(),
                    Container2No = tbContNo2.Text.Trim(),
                    TransporterInt = transporterIntCode,
                    TransporterEks = tbTransporterEks.Text.Trim(),
                    Note = tbNote.Text.Trim(),
                    Username = lblUsername.Text,
                    Status = "WEIGHIN"
                };

                if (!_sapRepo.SaveWeighInUDO(weighInData, out string sapError))
                {
                    MessageBox.Show("Gagal menyimpan data ke SAP: " + sapError, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Data added. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (!_printService.PrintTicket(tbTiketNum.Text.Trim(), TicketPrintType.Masuk, out string printError))
                {
                    MessageBox.Show($"Gagal mencetak tiket:\n{printError}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                ClearAllField();
                DisableFieldWB();
                tbBaseDoc.Focus();
                btnCariBaseDoc.Visible = true;
                btnRefreshData.Visible = false;
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
            // cbTMManual hidden - not enabled
            tbSealNo.Enabled = true;
            tbDONo.Enabled = true;
            tbNote.Enabled = true;
            tbContNo1.Enabled = true;
            tbContNo2.Enabled = true;
            btnTimbangMasuk.Enabled = true;
            btnWBSave.Enabled = true;
            cbTransInt.Enabled = true;
            tbTransporterEks.Enabled = true;
        }

        private void DisableFieldWB()
        {
            tbNopol.Enabled = false;
            tbNamaSupir.Enabled = false;
            tbSIMSupir.Enabled = false;
            tbJnsTruck.Enabled = false;
            // cbTMManual hidden - not disabled
            btnTimbangMasuk.Enabled = false;
            btnWBSave.Enabled = false;
            tbNoDokPabean.Enabled = false;
            tbContNo1.Enabled = false;
            tbContNo2.Enabled = false;
            tbSealNo.Enabled = false;
            tbDONo.Enabled = false;
            tbNote.Enabled = false;
            cbTransInt.Enabled = false;
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
            dtTglDokPabean.Value = DateTime.Now;
            // cbTMManual hidden - not cleared
            cbTransInt.Text = string.Empty;
            tbTransporterEks.Text = string.Empty;
            noKontrak = "";
            lblValDocEntDOSAP.Text = string.Empty;
            lblValLineNumDOSAP.Text = string.Empty;
        }

        private void LoadDataComboBox()
        {
            try
            {
                DataTable dt = _hanaRepo.GetTransporterInternal();
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

        private void cbTransInt_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Event wired in Designer
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _serialPortService?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
