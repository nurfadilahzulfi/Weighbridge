using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Weighbridge.Core.Config;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class WeighbridgeForm : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;
        private readonly IWeighInService _weighInService;
        private readonly IDocumentLookupService _docLookupService;
        private readonly IPabeanService _pabeanService;
        private readonly IPrintService _printService;
        private readonly SerialPortService _serialPortService;

        private WeighStatusForm _statusForm;

        public string timbangType = "A";

        private string noKontrak = "";
        private double sisaKuotaPO = 0;
        private double totalKuotaPO = 0;
        private string statusDocPO = "";
        private double _sisaKuotaSAP = 0;
        private double _pendingCommittedNetto = 0;

        private List<DocumentLookupItem> _cachedOpenDocuments = new List<DocumentLookupItem>();
        private bool _isSelectingSuggestion = false;
        private Timer _autoRefreshTimer;
        private DateTime _lastRefreshTime = DateTime.MinValue;

        public WeighbridgeForm()
            : this(new HanaRepository(), new SapRepository(), new PrintService(), new SerialPortService())
        {
        }

        public WeighbridgeForm(
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
            _weighInService = new WeighInService(_hanaRepo, _sapRepo);
            _docLookupService = new DocumentLookupService(_hanaRepo);
            _pabeanService = new PabeanService(_hanaRepo);
        }

        private void WeighbridgeForm_Load(object sender, EventArgs e)
        {
            InitializeSerialPort();
            LoadDataKodeDokumenPabean();
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
                dtTglDokPabean.CustomFormat = dtTglDokPabean.Checked ? "yyyy-MM-dd" : " ";
            };

            dtTglSPB.ValueChanged += (s, ev) =>
            {
                dtTglSPB.CustomFormat = dtTglSPB.Checked ? "yyyy-MM-dd" : " ";
            };

            cbTransInt.Enabled = false;
            cbTMManual.Visible = AppFeatures.AllowManualWeighing;
            cbTMManual.Enabled = false;
            cbTMManual.Checked = false;
            timbangType = "A";

            if (cbDocType.Items.Count > 0 && cbDocType.SelectedIndex < 0)
            {
                cbDocType.SelectedIndex = 0;
            }
            cbDocType.SelectedIndexChanged += cbDocType_SelectedIndexChanged;

            if (lstSearchSuggestions != null)
            {
                lstSearchSuggestions.BringToFront();
            }
            LoadOpenDocuments();

            tbBaseDoc.Enter += tbBaseDoc_Enter;
            tbBaseDoc.Click += tbBaseDoc_Click;

            InitializeSplitKontrakControls();
            ResponsiveLayoutHelper.Attach(this);
            WatermarkHelper.Apply(this);

            tbBrutoKebun.TextChanged += (s, ev) => 
            { 
                TryAutoCalculateNettoKebun(); 
                if (_isSplitKontrak) UpdateSplitKalkulasiLabel(); 
            };
            tbTaraKebun.TextChanged += (s, ev) => 
            { 
                TryAutoCalculateNettoKebun(); 
                if (_isSplitKontrak) UpdateSplitKalkulasiLabel(); 
            };
            tbNettoKebun.TextChanged += (s, ev) => 
            { 
                if (_isSplitKontrak) 
                {
                    OnNettoKebunChangedForSplit();
                    UpdateSplitKalkulasiLabel(); 
                }
            };

            _autoRefreshTimer = new Timer();
            _autoRefreshTimer.Interval = 5000;
            _autoRefreshTimer.Tick += (s, ev) =>
            {
                if (!_isSelectingSuggestion && !tbBaseDoc.Focused && (DateTime.Now - _lastRefreshTime).TotalSeconds >= 5)
                {
                    LoadOpenDocuments(silent: true);
                }
            };
            _autoRefreshTimer.Start();
        }

        private void InitializeSerialPort()
        {
            _serialPortService.Initialize();

            _serialPortService.OnWeightReceived += (weight) =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)delegate
                {
                    tbQtyMasuk.Text = weight;
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

        private void cbTMManual_CheckedChanged_1(object sender, EventArgs e)
        {
            bool manual = cbTMManual.Checked;
            timbangType = manual ? "M" : "A";
            tbQtyMasuk.Enabled = manual;
            tbQtyMasuk.Text = "0";
            btnTimbangMasuk.Visible = !manual;
        }

        private void tbQtyMasuk_KeyPress(object sender, KeyPressEventArgs e)
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

        private static double ParseWeightInput(string text) => DataConversionHelper.ParseWeightInput(text);

        private void tbQtyMasuk_TextChanged(object sender, EventArgs e)
        {
            tbQtyMasuk.ForeColor = SystemColors.WindowText;
            if (tbNopol.Enabled && IsPabeanLengkap())
            {
                btnWBSave.Enabled = true;
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
            if (!char.IsLetterOrDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '-' && e.KeyChar != '/' && e.KeyChar != ' ' && e.KeyChar != '.' && e.KeyChar != '_')
            {
                e.Handled = true;
            }
        }

        private void btnTimbangMasuk_Click(object sender, EventArgs e)
        {
            if (!IsPabeanLengkap())
            {
                ShowPabeanBelumLengkap();
                return;
            }

            try
            {
                _serialPortService.StartReading();
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
                _serialPortService.StartReading();
                ShowReadingDialog("Weigh Out");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private string GenerateTiketNumber() => _weighInService.GenerateTicketNumber();

        private void PrintWeighbridgeTicket(string ticketNumber)
        {
            if (_printService.PrintTicket(ticketNumber, TicketPrintType.Masuk, out string error))
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

            if (string.IsNullOrEmpty(tbBaseDoc.Text)) return;

            string docType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text);
            string vendorRef = tbBaseDoc.Text;

            var dt = _hanaRepo.SearchBaseDocument(docType, vendorRef);
            if (dt.Rows.Count == 0)
            {
                // SP di TANIMASLIVE hanya mencari Vendor Ref: No. PO/SO dicari di daftar dokumen terbuka
                if (_cachedOpenDocuments.Count == 0) LoadOpenDocuments(silent: true);
                var exact = DocumentLookupFilter.FindExact(_cachedOpenDocuments, vendorRef);
                if (exact != null)
                {
                    SelectDocumentItem(exact);
                    return;
                }

                MessageBox.Show("Document SAP tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableFieldWB();
                ClearAllField();
                return;
            }

            foreach (DataRow row in dt.Rows)
            {
                tbTiketNum.Text = GenerateTiketNumber();
                tbTiketNum.Enabled = false;

                int docEntry = DataConversionHelper.SafeToInt(row["DOCENTRY"]);
                string docNum = row["DOCNUM"]?.ToString() ?? "";
                vendorRef = row["VENDORREF"]?.ToString() ?? "";
                noKontrak = DataConversionHelper.NormalizeKontrak(row["KONTRAK"]);
                string itemCode = row["ITEMCODE"]?.ToString() ?? "";
                int linenumSAP = DataConversionHelper.SafeToInt(row["LINENUM"]);

                _vendorRefK1 = vendorRef;
                _itemCodeK1 = itemCode;
                _isSelectingSuggestion = true;
                tbBaseDoc.Text = DataConversionHelper.PreferVendorRef(vendorRef, docNum);
                _isSelectingSuggestion = false;
                tbBaseSAP.Text = docNum;
                tbCommodity.Text = itemCode;
                lbValDocEntSAP.Text = docEntry.ToString();
                lblValLineNumSAP.Text = linenumSAP.ToString();

                var quota = _weighInService.CheckDocQuota(docType, docEntry, linenumSAP);
                statusDocPO = quota.HeaderStatus;
                sisaKuotaPO = quota.OpenQty;
                totalKuotaPO = quota.TotalQty;

                if (statusDocPO == "C" || quota.LineStatus == "C")
                {
                    MessageBox.Show($"Dokumen {docType} #{docNum} sudah berstatus CLOSED di SAP B1!\nDokumen ini tidak dapat diproses untuk penimbangan.",
                        "Dokumen Closed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DisableFieldWB();
                    ClearAllField();
                    return;
                }

                var pabean = _pabeanService.GetPabeanData(docType, docEntry);
                PopulatePabeanForm(pabean);

                if (!ValidatePendingTickets(docType, docNum, noKontrak, vendorRef))
                {
                    DisableFieldWB();
                    ClearAllField();
                    return;
                }

                string infoMsg = $"Document SAP ditemukan.\n\n" +
                                 $"No. Dokumen : {docNum}\n" +
                                 (string.IsNullOrEmpty(noKontrak) ? "" : $"No. Kontrak : {noKontrak}\n") +
                                 (totalKuotaPO > 0 ? 
                                     $"Total Kuota SAP    : {DocumentLookupItem.FormatKg(totalKuotaPO)} kg\n" +
                                     $"Sisa Kuota SAP     : {DocumentLookupItem.FormatKg(_sisaKuotaSAP > 0 ? _sisaKuotaSAP : sisaKuotaPO)} kg\n" +
                                     (_pendingCommittedNetto > 0 ? $"Muatan Pending WB  : {DocumentLookupItem.FormatKg(_pendingCommittedNetto)} kg\n" : "") +
                                     $"Sisa Kuota Efektif : {DocumentLookupItem.FormatKg(sisaKuotaPO)} kg" 
                                     : "");

                MessageBox.Show(infoMsg.Trim(), "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);

                EnableFieldWB();
                ApplyPabeanFieldState();

                btnCariBaseDoc.Visible = false;
                btnRefreshData.Visible = true;

                if (cbDocType.SelectedItem?.ToString() == "PO" && sisaKuotaPO <= 0)
                {
                    string alertReason = _pendingCommittedNetto > 0 
                        ? $"Sisa Kuota Efektif PO ini adalah 0 kg (Habis terpakai oleh kuota/muatan truk pending di pabrik sebesar {DocumentLookupItem.FormatKg(_pendingCommittedNetto)} kg).\n\n"
                        : "Sisa Kuota PO ini adalah 0 kg (HABIS).\n\n";
                    MessageBox.Show(
                        $"PERHATIAN: {alertReason}" +
                        "PO ini tidak bisa menerima muatan lagi.\nPilih PO kontrak lain (sesuai SPB supplier) sebagai Base Document.",
                        "Kuota PO Habis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (cbSplitKontrak.Checked && string.IsNullOrEmpty(tbBaseDocK2.Text))
                {
                    cbSplitKontrak.Checked = false;
                }
            }
        }

        private List<PendingTicketInfo> GetPendingTicketsForDoc(string docType, string docNum, string noKontrak, string vendorRef)
        {
            var pendingTickets = _weighInService.GetPendingTickets(docType, docNum);
            if (pendingTickets.Count == 0 && !string.IsNullOrEmpty(noKontrak))
            {
                pendingTickets = _weighInService.GetPendingTickets(docType, noKontrak);
            }
            if (pendingTickets.Count == 0 && !string.IsNullOrEmpty(vendorRef))
            {
                pendingTickets = _weighInService.GetPendingTickets(docType, vendorRef);
            }
            return pendingTickets;
        }

        private bool ValidatePendingTickets(string docType, string docNum, string noKontrak, string vendorRef)
        {
            var pendingTickets = GetPendingTicketsForDoc(docType, docNum, noKontrak, vendorRef);

            _pendingCommittedNetto = 0;
            _sisaKuotaSAP = sisaKuotaPO;

            if (pendingTickets.Count > 0)
            {
                _pendingCommittedNetto = WeighbridgeCalculator.SumPendingNetto(pendingTickets);
                sisaKuotaPO = WeighbridgeCalculator.CalculateEffectiveQuota(_sisaKuotaSAP, _pendingCommittedNetto);

                var sbPending = new StringBuilder();
                sbPending.AppendLine($"PERHATIAN: Dokumen/Kontrak ini memiliki {pendingTickets.Count} transaksi penimbangan sebelumnya yang BELUM SELESAI DI-GRPO (masih di dalam pabrik):\n");
                foreach (var pt in pendingTickets)
                {
                    sbPending.AppendLine($"• No. Tiket     : {pt.TiketNum}");
                    sbPending.AppendLine($"  No. Polisi    : {pt.Nopol} (Supir: {pt.NamaSupir})");
                    sbPending.AppendLine($"  Estimasi Netto: {DocumentLookupItem.FormatKg(pt.Netto)} KG{(pt.Netto <= 0 ? " (data SPB kebun kosong)" : "")}");
                    sbPending.AppendLine($"  Status        : {pt.Status}\n");
                }
                sbPending.AppendLine($"Total Muatan Pending : {DocumentLookupItem.FormatKg(_pendingCommittedNetto)} KG");
                sbPending.AppendLine($"Sisa Kuota Resmi SAP : {DocumentLookupItem.FormatKg(_sisaKuotaSAP)} KG");
                sbPending.AppendLine($"Sisa Kuota Efektif K1: {DocumentLookupItem.FormatKg(sisaKuotaPO)} KG\n");
                sbPending.AppendLine("Catatan: Kuota efektif K1 otomatis disesuaikan dengan mengurangi muatan pending di atas agar perhitungan Split Kontrak dan GRPO akurat.");
                sbPending.AppendLine("Apakah Anda ingin TETAP MELANJUTKAN proses timbang masuk untuk dokumen ini?");

                var resConfirm = MessageBox.Show(sbPending.ToString(), "Peringatan Dokumen Belum Selesai GRPO", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (resConfirm == DialogResult.No)
                {
                    return false;
                }
            }
            return true;
        }

        private void PopulatePabeanForm(PabeanData pabean)
        {
            if (pabean == null) return;

            if (!string.IsNullOrEmpty(pabean.NomorSPB)) tbNomorSPB.Text = pabean.NomorSPB;
            if (pabean.TanggalSPB.HasValue)
            {
                dtTglSPB.Checked = true;
                dtTglSPB.Value = pabean.TanggalSPB.Value;
                dtTglSPB.CustomFormat = "yyyy-MM-dd";
            }
            if (!string.IsNullOrEmpty(pabean.StatusJalur)) cbStatusJalur.Text = pabean.StatusJalur;
            if (!string.IsNullOrEmpty(pabean.FasilitasPabean)) cbFasilitasPabean.Text = pabean.FasilitasPabean;
            if (!string.IsNullOrEmpty(pabean.KodeDokumen)) cmbKodeDokPabean.Text = pabean.KodeDokumen;
            if (!string.IsNullOrEmpty(pabean.NomorDokumen)) tbNoDokPabean.Text = pabean.NomorDokumen;
            if (pabean.TanggalDokumen.HasValue)
            {
                dtTglDokPabean.Checked = true;
                dtTglDokPabean.Value = pabean.TanggalDokumen.Value;
                dtTglDokPabean.CustomFormat = "yyyy-MM-dd";
            }
            if (!string.IsNullOrEmpty(pabean.NomorAju)) tbNomorAju.Text = pabean.NomorAju;
            if (!string.IsNullOrEmpty(pabean.NoKontrak)) noKontrak = pabean.NoKontrak;
        }

        private bool ValidasiDokumenPabean()
        {
            var val = _pabeanService.Validate(
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

        private bool IsPabeanLengkap()
        {
            return !string.IsNullOrWhiteSpace(cmbKodeDokPabean.Text)
                && !string.IsNullOrWhiteSpace(tbNoDokPabean.Text)
                && dtTglDokPabean.CustomFormat != " ";
        }

        private string GetPabeanKurang()
        {
            var kurang = new List<string>();
            if (string.IsNullOrWhiteSpace(cmbKodeDokPabean.Text)) kurang.Add("Kode Dokumen");
            if (string.IsNullOrWhiteSpace(tbNoDokPabean.Text)) kurang.Add("Nomor Dokumen");
            if (dtTglDokPabean.CustomFormat == " ") kurang.Add("Tanggal Dokumen");
            return string.Join(", ", kurang);
        }

        private void ShowPabeanBelumLengkap()
        {
            MessageBox.Show(
                $"Dokumen pabean untuk PO/SO ini belum lengkap di SAP.\nBelum diisi: {GetPabeanKurang()}.\n\n" +
                "Timbang Masuk tidak bisa dilakukan. Lengkapi dokumen pabean di SAP terlebih dahulu, lalu pilih ulang dokumen ini.",
                "Dokumen Pabean Belum Lengkap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ApplyPabeanFieldState()
        {
            bool isPabeanLengkap = IsPabeanLengkap();

            gbPabean.Enabled = isPabeanLengkap;
            btnTimbangMasuk.Enabled = isPabeanLengkap;
            cbTMManual.Enabled = AppFeatures.AllowManualWeighing && isPabeanLengkap;
            if (!isPabeanLengkap) cbTMManual.Checked = false;
            btnWBSave.Enabled = isPabeanLengkap;
            gbPabean.Text = isPabeanLengkap
                ? "Informasi Pabean"
                : "Informasi Pabean (Belum diisi di SAP - Harap lengkapi di SAP terlebih dahulu)";

            if (!isPabeanLengkap) ShowPabeanBelumLengkap();
        }

        private void TryAutoCalculateNettoKebun()
        {
            double bruto = ParseWeightInput(tbBrutoKebun.Text);
            double tara = ParseWeightInput(tbTaraKebun.Text);
            if (bruto > 0 && tara > 0 && bruto >= tara)
            {
                double netto = bruto - tara;
                tbNettoKebun.Text = DocumentLookupItem.FormatKg(netto);
            }
        }

        private void btnWBSave_Click(object sender, EventArgs e)
        {
            if (!ValidasiDokumenPabean()) return;

            if (DataConversionHelper.SafeToDouble(tbQtyMasuk.Text) <= 0)
            {
                MessageBox.Show(
                    "Berat masuk belum ditimbang (0 atau kosong).\nLakukan Timbang Masuk terlebih dahulu sebelum menyimpan.",
                    "Validasi Berat WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string docTypeSave = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text);
            if (WeighbridgeCalculator.IsNettoKebunMissing(docTypeSave, GetTotalNettoKebun()))
            {
                MessageBox.Show(
                    "Netto SPB Kebun wajib diisi untuk dokumen PO.\n" +
                    "Isi Bruto dan Tara Kebun (netto dihitung otomatis) atau isi Netto Kebun sesuai SPB.\n\n" +
                    "Netto SPB dipakai untuk menentukan apakah truk wajib Split Kontrak dan untuk menghitung sisa kuota truk berikutnya.",
                    "Data SPB Kebun Wajib Diisi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbBrutoKebun.Focus();
                return;
            }

            double bK = ParseWeightInput(tbBrutoKebun.Text);
            double tK = ParseWeightInput(tbTaraKebun.Text);
            double nK = ParseWeightInput(tbNettoKebun.Text);

            if (bK > 0 && tK > 0 && tK > bK)
            {
                MessageBox.Show(
                    $"Tara Kebun ({DocumentLookupItem.FormatKg(tK)} kg) tidak boleh lebih besar dari Bruto Kebun ({DocumentLookupItem.FormatKg(bK)} kg)!\n" +
                    "Mohon periksa kembali data timbang kebun yang dimasukkan.",
                    "Validasi Data Kebun", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbTaraKebun.Focus();
                return;
            }

            if (tK > 0 && nK > 0 && tK > nK)
            {
                var confirm = MessageBox.Show(
                    $"PERHATIAN: Angka Tara Kebun ({DocumentLookupItem.FormatKg(tK)} kg) lebih besar dari Netto Kebun ({DocumentLookupItem.FormatKg(nK)} kg).\n\n" +
                    "Apakah Anda yakin data Tara dan Netto Kebun tidak tertukar?\n\n" +
                    "Pilih 'Yes' untuk tetap melanjutkan, atau 'No' untuk memeriksa kembali.",
                    "Konfirmasi Data Kebun", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (confirm != DialogResult.Yes)
                {
                    tbTaraKebun.Focus();
                    return;
                }
            }

            if (cbDocType.SelectedItem?.ToString() == "PO" && sisaKuotaPO <= 0)
            {
                MessageBox.Show(
                    "Sisa kuota PO ini sudah HABIS (0 kg), data tidak bisa disimpan.\n" +
                    "Pilih PO kontrak lain (sesuai SPB supplier) sebagai Base Document.",
                    "Kuota PO Habis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbBaseDoc.Focus();
                return;
            }

            double nettoKebunTotal = GetTotalNettoKebun();
            if (cbDocType.SelectedItem?.ToString() == "PO" && !_isSplitKontrak &&
                WeighbridgeCalculator.RequiresSplitKontrak(nettoKebunTotal, sisaKuotaPO))
            {
                MessageBox.Show(
                    $"Netto Kebun ({DocumentLookupItem.FormatKg(nettoKebunTotal)} kg) MELEBIHI sisa kuota efektif PO K1 ({DocumentLookupItem.FormatKg(sisaKuotaPO)} kg).\n" +
                    $"Kekurangan: {DocumentLookupItem.FormatKg(nettoKebunTotal - sisaKuotaPO)} kg.\n\n" +
                    "Sesuai SOP wajib Split Kontrak. Mode Split Kontrak diaktifkan, silakan pilih Kontrak Ke-2.",
                    "Split Kontrak Diperlukan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbSplitKontrak.Checked = true;
                tbBaseDocK2.Focus();
                return;
            }

            if (!_isSplitKontrak)
            {
                DialogResult result = MessageBox.Show("Pastikan semua data sudah benar. Lanjut simpan data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes) return;
            }

            try
            {
                string wbStatus = "WEIGHIN";
                bool isSaved = false;

                if (!EnsureTicketNumberUnused()) return;

                if (_isSplitKontrak)
                {
                    string missing = GetMissingSplitKontrakMessage(out Control focus);
                    if (missing.Length > 0)
                    {
                        MessageBox.Show(missing, "Split Kontrak - Kontrak Belum Dipilih", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        focus?.Focus();
                        return;
                    }
                    isSaved = SaveSplitWBUDO();
                }
                else
                {
                    isSaved = AddWBUDO(wbStatus);
                }

                if (!isSaved) return;

                PrintWeighbridgeTicket(tbTiketNum.Text);

                ClearAllField();
                DisableFieldWB();
                tbBaseDoc.Focus();
                btnCariBaseDoc.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureTicketNumberUnused()
        {
            string tiket = tbTiketNum.Text?.Trim() ?? "";
            try
            {
                if (_hanaRepo.SearchTicketByTiketNum(tiket).Rows.Count == 0) return true;

                string tiketBaru = GenerateTiketNumber();
                if (string.IsNullOrWhiteSpace(tiketBaru) || string.Equals(tiketBaru, tiket, StringComparison.OrdinalIgnoreCase) ||
                    _hanaRepo.SearchTicketByTiketNum(tiketBaru).Rows.Count > 0)
                {
                    MessageBox.Show(
                        $"Nomor tiket {tiket} sudah dipakai record lain dan nomor tiket baru tidak bisa dibuat.\nData belum disimpan, coba simpan lagi.",
                        "Nomor Tiket Sudah Dipakai", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                tbTiketNum.Text = tiketBaru;
                MessageBox.Show(
                    $"Nomor tiket {tiket} sudah dipakai truk lain sebelum data ini disimpan.\nNomor tiket diganti menjadi {tiketBaru}.",
                    "Nomor Tiket Diganti", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memeriksa nomor tiket di SAP, data belum disimpan:\n" + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void tbBaseDoc_Enter(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastRefreshTime).TotalSeconds >= 3)
            {
                LoadOpenDocuments(silent: true);
            }
        }

        private void tbBaseDoc_Click(object sender, EventArgs e)
        {
            if ((DateTime.Now - _lastRefreshTime).TotalSeconds >= 3)
            {
                LoadOpenDocuments(silent: true);
            }
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?\nForm akan dikosongkan dan data PO/SO dimuat ulang dari SAP.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;

            ClearAllField();
            DisableFieldWB();
            if (cbDocType.Items.Count > 0) cbDocType.SelectedIndex = 0;
            btnCariBaseDoc.Visible = true;
            btnRefreshData.Visible = true;

            _cachedOpenDocuments.Clear();
            if (LoadOpenDocuments(silent: false))
            {
                MessageBox.Show("Form dikosongkan dan data PO/SO dari SAP berhasil diperbarui.", "Refresh Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            tbBaseDoc.Focus();
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
            cbTMManual.Enabled = AppFeatures.AllowManualWeighing;
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
            btnWBSave.Enabled = true;
            cbTransInt.Enabled = true;
            tbTransporterEks.Enabled = true;
            bool splitTersedia = IsSplitKontrakTersedia();
            cbSplitKontrak.Enabled = splitTersedia;
            tbBaseDocK2.Enabled = splitTersedia;
            btnCariKontrakK2.Enabled = splitTersedia;
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
            cbTMManual.Checked = false;
            cbTMManual.Enabled = false;
            btnTimbangMasuk.Enabled = false;
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
            cbSplitKontrak.Enabled = false;
            cbSplitKontrak.Checked = false;
            tbBaseDocK2.Enabled = false;
            btnCariKontrakK2.Enabled = false;
            gbPabean.Enabled = false;
            gbPabean.Text = "Informasi Pabean";
        }

        private void ClearAllField()
        {
            lbValDocEntSAP.Text = string.Empty;
            lblValLineNumSAP.Text = string.Empty;
            tbBaseSAP.Text = string.Empty;
            noKontrak = "";
            sisaKuotaPO = 0;
            totalKuotaPO = 0;
            statusDocPO = "";
            _sisaKuotaSAP = 0;
            _pendingCommittedNetto = 0;
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
            dtTglDokPabean.Checked = false;
            dtTglDokPabean.CustomFormat = " ";
            dtTglSPB.Value = DateTime.Now;
            dtTglSPB.Checked = false;
            dtTglSPB.CustomFormat = " ";
            cbStatusJalur.SelectedIndex = -1;
            cbStatusJalur.Text = string.Empty;
            cbFasilitasPabean.SelectedIndex = -1;
            cbFasilitasPabean.Text = string.Empty;
            cbTransInt.Text = string.Empty;
            tbTransporterEks.Text = string.Empty;
            if (lstSearchSuggestions != null)
            {
                lstSearchSuggestions.Visible = false;
                lstSearchSuggestions.Items.Clear();
            }

            cbSplitKontrak.Checked = false;
            _isSplitKontrak = false;
            ClearSplitKontrakK2();
            _vendorRefK1 = "";
            _itemCodeK1 = "";
            gbPabean.Text = "Informasi Pabean";
        }

        private WeighInData BuildWeighInData(
            int docEntry,
            int lineNum,
            string docNum,
            string vendorRef,
            string kontrak,
            double brutoKebun,
            double taraKebun,
            double nettoKebun,
            double beratMasuk,
            string tiket,
            string wbStatus)
        {
            return new WeighInData
            {
                DocType = cbDocType.SelectedItem != null ? cbDocType.SelectedItem.ToString() : (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text),
                DocNum = docNum ?? "",
                VendorRef = vendorRef ?? "",
                DocEntry = docEntry,
                LineNum = lineNum,
                TiketNum = tiket ?? "",
                Commodity = tbCommodity.Text ?? "",
                NoKontrak = kontrak ?? "",
                Nopol = tbNopol.Text ?? "",
                NamaSupir = tbNamaSupir.Text ?? "",
                NoSimSupir = tbSIMSupir.Text ?? "",
                JenisTruck = tbJnsTruck.Text ?? "",
                BeratMasuk = beratMasuk,
                BrutoKebun = brutoKebun,
                TaraKebun = taraKebun,
                NettoKebun = nettoKebun,
                FfaKebun = tbFFAKebun.Text ?? "",
                MoistKebun = tbMOISTKebun.Text ?? "",
                LabFfa = tbFFA.Text ?? "",
                LabMoist = tbMOIST.Text ?? "",
                LabImp = tbIMP.Text ?? "",
                LabDobi = tbDOBI.Text ?? "",
                LabCarotine = tbCAROTINE.Text ?? "",
                KodeDokumenPabean = cmbKodeDokPabean.Text?.Trim() ?? "",
                NoDokumenPabean = tbNoDokPabean.Text ?? "",
                TanggalDokumenPabean = dtTglDokPabean.Checked ? (DateTime?)dtTglDokPabean.Value : null,
                NomorSpb = tbNomorSPB.Text ?? "",
                TanggalSpb = dtTglSPB.Checked ? (DateTime?)dtTglSPB.Value : null,
                NomorAju = tbNomorAju.Text ?? "",
                SealNo = tbSealNo.Text ?? "",
                DoNo = tbDONo.Text ?? "",
                Container1No = tbContNo1.Text ?? "",
                Container2No = tbContNo2.Text ?? "",
                Note = tbNote.Text ?? "",
                Username = lblUsername.Text ?? "",
                Status = wbStatus ?? "WEIGHIN",
                StatusJalur = cbStatusJalur.Text ?? "",
                FasilitasPabean = cbFasilitasPabean.Text ?? "",
                TransporterInt = cbTransInt.SelectedValue != null ? cbTransInt.SelectedValue.ToString() : "",
                TransporterEks = tbTransporterEks.Text ?? ""
            };
        }

        private bool AddWBUDO(string wbStatus)
        {
            try
            {
                var data = BuildWeighInData(
                    docEntry: DataConversionHelper.SafeToInt(lbValDocEntSAP.Text),
                    lineNum: DataConversionHelper.SafeToInt(lblValLineNumSAP.Text),
                    docNum: tbBaseSAP.Text,
                    vendorRef: string.IsNullOrEmpty(_vendorRefK1) ? tbBaseDoc.Text : _vendorRefK1,
                    kontrak: noKontrak,
                    brutoKebun: ParseWeightInput(tbBrutoKebun.Text),
                    taraKebun: ParseWeightInput(tbTaraKebun.Text),
                    nettoKebun: GetTotalNettoKebun(),
                    beratMasuk: DataConversionHelper.SafeToDouble(tbQtyMasuk.Text),
                    tiket: tbTiketNum.Text,
                    wbStatus: wbStatus
                );

                if (_weighInService.SaveWeighIn(data, out string error))
                {
                    MessageBox.Show("Data added. Check on Weighbridge Data in SAP", "Success - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }

                MessageBox.Show("Error : " + error, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error : " + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void LoadDataComboBox()
        {
            try
            {
                var dt = _hanaRepo.GetTransporterInternal();
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

        private void cbDocType_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadOpenDocuments();
            if (lstSearchSuggestions != null)
            {
                lstSearchSuggestions.Visible = false;
                lstSearchSuggestions.Items.Clear();
            }

            if (!string.IsNullOrWhiteSpace(tbBaseDoc.Text))
            {
                tbBaseDoc_TextChanged(sender, e);
            }
        }

        private bool LoadOpenDocuments(bool silent = false)
        {
            string docType = cbDocType.SelectedItem != null ? cbDocType.SelectedItem.ToString() : "PO";

            try
            {
                _cachedOpenDocuments = _docLookupService.GetOpenDocuments(docType, forceRefresh: true);
                _lastRefreshTime = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadOpenDocuments error: " + ex.Message);
                if (!silent)
                {
                    MessageBox.Show(
                        "Gagal memuat daftar dokumen dari SAP:\n" + ex.Message + "\n\nPastikan koneksi ke SAP HANA aktif dan coba lagi.",
                        "Error Load Dokumen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        private void tbBaseDoc_TextChanged(object sender, EventArgs e)
        {
            if (_isSelectingSuggestion) return;

            string query = tbBaseDoc.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                if (lstSearchSuggestions != null)
                {
                    lstSearchSuggestions.Visible = false;
                    lstSearchSuggestions.Items.Clear();
                }
                return;
            }

            if (_cachedOpenDocuments.Count == 0)
            {
                bool loaded = LoadOpenDocuments();
                if (!loaded) return;
            }

            var results = DocumentLookupFilter.Filter(_cachedOpenDocuments, query);

            if (lstSearchSuggestions != null)
            {
                lstSearchSuggestions.Items.Clear();

                if (results.Count > 0)
                {
                    foreach (var item in results)
                    {
                        lstSearchSuggestions.Items.Add(item);
                    }
                    lstSearchSuggestions.BringToFront();
                    lstSearchSuggestions.Visible = true;
                }
                else
                {
                    lstSearchSuggestions.Visible = false;
                }
            }
        }

        private void tbBaseDoc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (lstSearchSuggestions != null && lstSearchSuggestions.Visible && lstSearchSuggestions.Items.Count > 0)
                {
                    lstSearchSuggestions.Focus();
                    lstSearchSuggestions.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (lstSearchSuggestions != null && lstSearchSuggestions.Visible && lstSearchSuggestions.SelectedItem != null)
                {
                    SelectDocumentItem((DocumentLookupItem)lstSearchSuggestions.SelectedItem);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (lstSearchSuggestions != null && lstSearchSuggestions.Visible && lstSearchSuggestions.Items.Count > 0)
                {
                    SelectDocumentItem((DocumentLookupItem)lstSearchSuggestions.Items[0]);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else
                {
                    btnCariBaseDoc_Click(sender, e);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (lstSearchSuggestions != null)
                {
                    lstSearchSuggestions.Visible = false;
                }
                e.Handled = true;
            }
        }

        private void lstSearchSuggestions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lstSearchSuggestions.SelectedItem != null)
                {
                    SelectDocumentItem((DocumentLookupItem)lstSearchSuggestions.SelectedItem);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstSearchSuggestions.Visible = false;
                tbBaseDoc.Focus();
                e.Handled = true;
            }
        }

        private void lstSearchSuggestions_Click(object sender, EventArgs e)
        {
            if (lstSearchSuggestions.SelectedItem != null)
            {
                SelectDocumentItem((DocumentLookupItem)lstSearchSuggestions.SelectedItem);
            }
        }

        private void SelectDocumentItem(DocumentLookupItem item)
        {
            if (item == null) return;

            string docType = cbDocType.SelectedItem != null ? cbDocType.SelectedItem.ToString() : "PO";

            _isSelectingSuggestion = true;
            _vendorRefK1 = item.NumAtCard;
            _itemCodeK1 = item.ItemCode ?? "";
            tbBaseDoc.Text = DataConversionHelper.PreferVendorRef(item.NumAtCard, item.DocNum);
            tbBaseSAP.Text = item.DocNum;
            lbValDocEntSAP.Text = item.DocEntry.ToString();
            lblValLineNumSAP.Text = item.LineNum.ToString();
            tbCommodity.Text = !string.IsNullOrEmpty(item.ItemName) ? item.ItemName : item.ItemCode;
            sisaKuotaPO = item.OpenQty;
            totalKuotaPO = item.Quantity;
            statusDocPO = item.DocStatus;

            var freshQuota = _hanaRepo.GetFreshQuota(docType, item.DocEntry, item.LineNum);
            sisaKuotaPO = freshQuota.openQty;
            totalKuotaPO = freshQuota.totalQty;

            tbTiketNum.Text = GenerateTiketNumber();
            tbTiketNum.Enabled = false;

            if (lstSearchSuggestions != null)
            {
                lstSearchSuggestions.Visible = false;
                lstSearchSuggestions.Items.Clear();
            }
            _isSelectingSuggestion = false;

            var pabean = _pabeanService.GetPabeanData(docType, item.DocEntry);
            PopulatePabeanForm(pabean);
            noKontrak = !string.IsNullOrEmpty(pabean?.NoKontrak) ? pabean.NoKontrak : (!string.IsNullOrEmpty(item.ContractNo) ? item.ContractNo : "");

            if (!ValidatePendingTickets(docType, item.DocNum, noKontrak, _vendorRefK1))
            {
                DisableFieldWB();
                ClearAllField();
                return;
            }

            EnableFieldWB();
            ApplyPabeanFieldState();
            btnCariBaseDoc.Visible = false;
            btnRefreshData.Visible = true;
            tbNopol.Focus();

            if (cbDocType.SelectedItem?.ToString() == "PO" && sisaKuotaPO <= 0)
            {
                string alertReason = _pendingCommittedNetto > 0 
                    ? $"Sisa Kuota Efektif PO ini adalah 0 kg (Habis terpakai oleh kuota/muatan truk pending di pabrik sebesar {DocumentLookupItem.FormatKg(_pendingCommittedNetto)} kg).\n\n"
                    : "Sisa Kuota PO ini adalah 0 kg (HABIS).\n\n";
                MessageBox.Show(
                    $"PERHATIAN: {alertReason}" +
                    "Seluruh muatan truk ini akan DIALIHKAN ke Kontrak Ke-2 (bukan split).\nSilakan pilih Kontrak Ke-2 pada field di bawah Base Document.",
                    "Kuota Habis - Wajib Split Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbSplitKontrak.Checked = true;
                tbBaseDocK2.Focus();
            }
            else if (cbSplitKontrak.Checked && string.IsNullOrEmpty(tbBaseDocK2.Text))
            {
                cbSplitKontrak.Checked = false;
            }
        }

        private void tbQtyMasuk_Leave(object sender, EventArgs e)
        {
        }

        private void cbTransInt_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}
