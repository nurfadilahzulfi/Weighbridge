using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class WeighbridgeForm
    {
        private sealed class SplitKontrakSlot
        {
            public readonly string Label;
            public int DocEntry;
            public int LineNum;
            public double SisaKuota;
            public double SisaKuotaSAP;
            public double PendingNetto;
            public double TotalKuota;
            public string NoKontrak = "";
            public string DocNum = "";
            public string VendorRef = "";
            public bool IsSelectingSuggestion;

            public SplitKontrakSlot(string label) { Label = label; }

            public bool IsSelected => DocEntry != 0 && !string.IsNullOrEmpty(DocNum);
            public string DisplayName => string.IsNullOrEmpty(NoKontrak) ? DocNum : NoKontrak;

            public void Clear()
            {
                DocEntry = 0;
                LineNum = 0;
                SisaKuota = 0;
                SisaKuotaSAP = 0;
                PendingNetto = 0;
                TotalKuota = 0;
                NoKontrak = "";
                DocNum = "";
                VendorRef = "";
            }
        }

        private bool _isSplitKontrak = false;
        private bool _isKontrakK3 = false;
        private readonly SplitKontrakSlot _k2 = new SplitKontrakSlot("K2");
        private readonly SplitKontrakSlot _k3 = new SplitKontrakSlot("K3");
        private string _vendorRefK1 = "";
        private string _itemCodeK1 = "";

        private Label lblNoKontrakK3;
        private CheckBox cbKontrakK3;
        private TextBox tbBaseDocK3;
        private Button btnCariKontrakK3;
        private ListBox lstSuggestionsK3;

        private Label lblNettoSPBK1;
        private TextBox tbNettoSPBK1;
        private Label lblNettoSPBK2;
        private TextBox tbNettoSPBK2;
        private Label lblNettoSPBK3;
        private TextBox tbNettoSPBK3;
        private bool _isUpdatingSplitInput = false;

        public void InitializeSplitKontrakControls()
        {
            lblNoKontrakK3 = new Label
            {
                Text = "Kontrak Ke-3",
                AutoSize = true,
                Location = new Point(18, 101),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.Black
            };

            cbKontrakK3 = new CheckBox
            {
                Text = "Split 3 Kontrak",
                AutoSize = true,
                Location = new Point(98, 100),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                ForeColor = Color.DarkOrange,
                Enabled = false,
                UseVisualStyleBackColor = true
            };

            tbBaseDocK3 = new TextBox
            {
                Location = new Point(20, 118),
                Size = new Size(269, 24),
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular),
                Enabled = false
            };

            btnCariKontrakK3 = new Button
            {
                Text = "🔄 Cari K3",
                Location = new Point(295, 117),
                Size = new Size(75, 25),
                Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular),
                Enabled = false,
                UseVisualStyleBackColor = true
            };

            lstSuggestionsK3 = new ListBox
            {
                Location = new Point(20, 143),
                Size = new Size(650, 160),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                FormattingEnabled = true,
                HorizontalScrollbar = true,
                ItemHeight = 15,
                Visible = false
            };

            lblNettoSPBK1 = CreateNettoSpbLabel("Netto SPB K1", 130);
            tbNettoSPBK1 = CreateNettoSpbTextBox(130);
            lblNettoSPBK2 = CreateNettoSpbLabel("Netto SPB K2", 224);
            tbNettoSPBK2 = CreateNettoSpbTextBox(224);
            lblNettoSPBK3 = CreateNettoSpbLabel("Netto SPB K3", 318);
            tbNettoSPBK3 = CreateNettoSpbTextBox(318);

            tbNettoSPBK1.TextChanged += tbNettoSPBK1_TextChanged;
            tbNettoSPBK2.TextChanged += tbNettoSPBK2_TextChanged;
            tbNettoSPBK3.TextChanged += tbNettoSPBK3_TextChanged;

            cbKontrakK3.CheckedChanged += cbKontrakK3_CheckedChanged;
            tbBaseDocK3.TextChanged += (s, e) => OnKontrakTextChanged(_k3, tbBaseDocK3, lstSuggestionsK3);
            tbBaseDocK3.KeyDown += (s, e) => OnKontrakTextKeyDown(_k3, lstSuggestionsK3, e);
            lstSuggestionsK3.Click += (s, e) => { if (lstSuggestionsK3.SelectedItem is DocumentLookupItem item) SelectDocumentItem(_k3, item); };
            lstSuggestionsK3.KeyDown += (s, e) => OnSuggestionKeyDown(_k3, tbBaseDocK3, lstSuggestionsK3, e);
            btnCariKontrakK3.Click += (s, e) => OnCariKontrakClick(_k3, tbBaseDocK3, lstSuggestionsK3);

            var added = new Control[]
            {
                lblNoKontrakK3, cbKontrakK3, tbBaseDocK3, btnCariKontrakK3,
                lblNettoSPBK1, tbNettoSPBK1, lblNettoSPBK2, tbNettoSPBK2, lblNettoSPBK3, tbNettoSPBK3,
                lstSuggestionsK3
            };
            int tab = btnCariKontrakK2.TabIndex + 1;
            foreach (Control c in new Control[] { cbKontrakK3, tbBaseDocK3, btnCariKontrakK3, tbNettoSPBK1, tbNettoSPBK2, tbNettoSPBK3 })
                c.TabIndex = tab++;

            foreach (Control c in added)
            {
                this.Controls.Add(c);
                c.BringToFront();
            }
            if (lstSuggestionsK2 != null) lstSuggestionsK2.BringToFront();
            if (lstSearchSuggestions != null) lstSearchSuggestions.BringToFront();

            UpdateSplitControlState();
        }

        private static Label CreateNettoSpbLabel(string text, int left)
        {
            return new Label
            {
                Text = text,
                Location = new Point(left, 146),
                Size = new Size(88, 15),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
                ForeColor = Color.DarkSlateBlue,
                Visible = false
            };
        }

        private static TextBox CreateNettoSpbTextBox(int left)
        {
            return new TextBox
            {
                Location = new Point(left, 163),
                Size = new Size(88, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Visible = false
            };
        }

        private void UpdateSplitControlState()
        {
            bool split = _isSplitKontrak;
            bool k3 = split && _isKontrakK3;

            if (cbKontrakK3 != null) cbKontrakK3.Enabled = split;
            if (tbBaseDocK3 != null) tbBaseDocK3.Enabled = split;
            if (btnCariKontrakK3 != null) btnCariKontrakK3.Enabled = split;

            if (lblNettoSPBK1 != null) lblNettoSPBK1.Visible = split;
            if (tbNettoSPBK1 != null) tbNettoSPBK1.Visible = split;
            if (lblNettoSPBK2 != null) lblNettoSPBK2.Visible = split;
            if (tbNettoSPBK2 != null) tbNettoSPBK2.Visible = split;
            if (lblNettoSPBK3 != null) lblNettoSPBK3.Visible = k3;
            if (tbNettoSPBK3 != null) tbNettoSPBK3.Visible = k3;
        }

        private double GetTotalNettoKebun()
        {
            double bruto = ParseWeightInput(tbBrutoKebun.Text);
            double tara = ParseWeightInput(tbTaraKebun.Text);
            double netto = ParseWeightInput(tbNettoKebun.Text);

            if (netto <= 0 && bruto > 0 && tara > 0 && bruto > tara)
            {
                netto = bruto - tara;
            }
            return netto;
        }

        private List<double> GetNettoSpbTetap()
        {
            var values = new List<double> { tbNettoSPBK1 != null ? ParseWeightInput(tbNettoSPBK1.Text) : 0 };
            if (_isKontrakK3) values.Add(tbNettoSPBK2 != null ? ParseWeightInput(tbNettoSPBK2.Text) : 0);
            return values;
        }

        private SplitKontrakSlot LastSlot => _isKontrakK3 ? _k3 : _k2;

        private void UpdateNettoSpbK3()
        {
            if (!_isKontrakK3 || tbNettoSPBK3 == null) return;

            double nettoKebun = GetTotalNettoKebun();
            double k1 = ParseWeightInput(tbNettoSPBK1.Text);
            double k2 = ParseWeightInput(tbNettoSPBK2.Text);
            double k3 = Math.Round(nettoKebun - k1 - k2, 2);
            _isUpdatingSplitInput = true;
            tbNettoSPBK3.Text = nettoKebun > 0 && k3 > 0 ? DocumentLookupItem.FormatKg(k3) : "";
            _isUpdatingSplitInput = false;
        }

        // Split 3: K3 diketik -> K2 = Netto SPB Kebun - K1 - K3 (jumlah ketiganya tetap = Netto SPB Kebun)
        private void tbNettoSPBK3_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingSplitInput || !_isSplitKontrak || !_isKontrakK3) return;

            double nettoKebun = GetTotalNettoKebun();
            double k1 = ParseWeightInput(tbNettoSPBK1.Text);
            double k3 = ParseWeightInput(tbNettoSPBK3.Text);
            if (nettoKebun > 0 && k3 >= 0 && k1 + k3 <= nettoKebun)
            {
                _isUpdatingSplitInput = true;
                double k2 = Math.Round(nettoKebun - k1 - k3, 2);
                tbNettoSPBK2.Text = k2 > 0 ? DocumentLookupItem.FormatKg(k2) : "0";
                _isUpdatingSplitInput = false;
            }

            UpdateSplitKalkulasiLabel();
        }

        private void tbNettoSPBK1_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingSplitInput || !_isSplitKontrak) return;

            if (_isKontrakK3)
            {
                UpdateNettoSpbK3();
            }
            else
            {
                double nettoKebun = GetTotalNettoKebun();
                double valK1 = ParseWeightInput(tbNettoSPBK1.Text);
                if (nettoKebun > 0 && valK1 >= 0 && valK1 <= nettoKebun && tbNettoSPBK2 != null)
                {
                    _isUpdatingSplitInput = true;
                    double valK2 = Math.Round(nettoKebun - valK1, 2);
                    tbNettoSPBK2.Text = valK2 > 0 ? DocumentLookupItem.FormatKg(valK2) : "0";
                    _isUpdatingSplitInput = false;
                }
            }

            UpdateSplitKalkulasiLabel();
        }

        private void tbNettoSPBK2_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingSplitInput || !_isSplitKontrak) return;

            if (_isKontrakK3)
            {
                UpdateNettoSpbK3();
            }
            else
            {
                double nettoKebun = GetTotalNettoKebun();
                double valK2 = ParseWeightInput(tbNettoSPBK2.Text);
                if (nettoKebun > 0 && valK2 >= 0 && valK2 <= nettoKebun && tbNettoSPBK1 != null)
                {
                    _isUpdatingSplitInput = true;
                    double valK1 = Math.Round(nettoKebun - valK2, 2);
                    tbNettoSPBK1.Text = valK1 > 0 ? DocumentLookupItem.FormatKg(valK1) : "0";
                    _isUpdatingSplitInput = false;
                }
            }

            UpdateSplitKalkulasiLabel();
        }

        public void OnNettoKebunChangedForSplit()
        {
            if (!_isSplitKontrak) return;

            if (_isKontrakK3)
            {
                UpdateNettoSpbK3();
            }
            else
            {
                double nettoKebun = GetTotalNettoKebun();
                double currentK1 = tbNettoSPBK1 != null ? ParseWeightInput(tbNettoSPBK1.Text) : 0;
                if (nettoKebun > 0 && currentK1 > 0 && currentK1 < nettoKebun && tbNettoSPBK2 != null)
                {
                    _isUpdatingSplitInput = true;
                    tbNettoSPBK2.Text = DocumentLookupItem.FormatKg(Math.Round(nettoKebun - currentK1, 2));
                    _isUpdatingSplitInput = false;
                }
            }
            UpdateSplitKalkulasiLabel();
        }

        private bool SaveSplitWBUDO()
        {
            double taraKebun = ParseWeightInput(tbTaraKebun.Text);
            double nettoKebun = GetTotalNettoKebun();
            double beratMasukAktual = ParseWeightInput(tbQtyMasuk.Text);

            var calc = WeighbridgeCalculator.CalculateSplitBySPB(nettoKebun, GetNettoSpbTetap(), taraKebun);
            if (!calc.NeedsSplit)
            {
                MessageBox.Show(calc.Error, "Split Kontrak - Netto SPB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (tbNettoSPBK1 != null) tbNettoSPBK1.Focus();
                return false;
            }

            if (calc.Netto[0] > sisaKuotaPO)
            {
                MessageBox.Show(
                    $"Netto SPB K1 ({DocumentLookupItem.FormatKg(calc.Netto[0])} kg) MELEBIHI sisa kuota efektif PO K1 ({DocumentLookupItem.FormatKg(sisaKuotaPO)} kg).\n\n" +
                    $"Maksimum Netto SPB K1 yang diizinkan adalah {DocumentLookupItem.FormatKg(sisaKuotaPO)} kg. Periksa kembali SPB Kontrak 1 dari supplier.",
                    "Validasi Kuota K1", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (tbNettoSPBK1 != null) tbNettoSPBK1.Focus();
                return false;
            }
            if (_isKontrakK3 && calc.Netto[1] > _k2.SisaKuota)
            {
                MessageBox.Show(
                    $"Netto SPB K2 ({DocumentLookupItem.FormatKg(calc.Netto[1])} kg) MELEBIHI sisa kuota efektif PO K2 ({DocumentLookupItem.FormatKg(_k2.SisaKuota)} kg).\n\n" +
                    $"Maksimum Netto SPB K2 yang diizinkan adalah {DocumentLookupItem.FormatKg(_k2.SisaKuota)} kg. Periksa kembali SPB Kontrak 2 dari supplier.",
                    "Validasi Kuota K2", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (tbNettoSPBK2 != null) tbNettoSPBK2.Focus();
                return false;
            }

            SplitKontrakSlot last = LastSlot;
            double nettoLast = calc.Netto[calc.Netto.Count - 1];
            var valLast = WeighbridgeCalculator.ValidateK2Quota(nettoLast, last.SisaKuota);
            if (!valLast.IsSufficient)
            {
                var warnResult = MessageBox.Show(
                    $"PERHATIAN: Sisa Kuota Kontrak {last.Label} ({DocumentLookupItem.FormatKg(last.SisaKuota)} kg) tidak mencukupi untuk Netto SPB {last.Label} ({DocumentLookupItem.FormatKg(nettoLast)} kg).\n\n" +
                    $"Kekurangan kuota {last.Label}: {DocumentLookupItem.FormatKg(valLast.Shortage)} kg.\n" +
                    $"Estimasi sisa kuota {last.Label} setelah alokasi akan minus: {DocumentLookupItem.FormatKg(valLast.RemainingQuotaAfterDeduction)} kg.\n\n" +
                    "Apakah Anda yakin ingin tetap melanjutkan simpan Split Kontrak ini?",
                    $"Peringatan Kuota {last.Label} Kurang", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

                if (warnResult != DialogResult.Yes)
                    return false;
            }

            var slots = new List<SplitKontrakSlot> { _k2 };
            if (_isKontrakK3) slots.Add(_k3);
            string namaK1 = string.IsNullOrEmpty(noKontrak) ? tbBaseSAP.Text : noKontrak;

            var konfirmasi = new System.Text.StringBuilder();
            konfirmasi.Append("Konfirmasi Split Kontrak (Referensi SPB):\n\n");
            konfirmasi.Append($"  Total Netto SPB : {DocumentLookupItem.FormatKg(nettoKebun)} kg\n");
            konfirmasi.Append("------------------------------------------------------\n");
            konfirmasi.Append($"  Kontrak K1 : {namaK1}\n");
            konfirmasi.Append($"    Netto SPB K1 : {DocumentLookupItem.FormatKg(calc.Netto[0])} kg (Sisa Kuota: {DocumentLookupItem.FormatKg(sisaKuotaPO)} kg)\n\n");
            for (int i = 0; i < slots.Count; i++)
            {
                SplitKontrakSlot slot = slots[i];
                bool isLast = i == slots.Count - 1;
                konfirmasi.Append($"  Kontrak {slot.Label} : {slot.DisplayName}\n");
                konfirmasi.Append($"    Netto SPB {slot.Label} : {DocumentLookupItem.FormatKg(calc.Netto[i + 1])} kg (Sisa Kuota: {DocumentLookupItem.FormatKg(slot.SisaKuota)} kg)" +
                                  (isLast && slots.Count > 1 ? " = sisa muatan" : "") + "\n" + (isLast ? "" : "\n"));
            }
            konfirmasi.Append("------------------------------------------------------\n");
            konfirmasi.Append($"Akan disimpan sebagai {calc.Netto.Count} record WB dengan tiket yang sama ({tbTiketNum.Text}).\nLanjutkan?");

            if (MessageBox.Show(konfirmasi.ToString(), "Konfirmasi Split Kontrak", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
                return false;

            try
            {
                var parts = new List<WeighInData>
                {
                    BuildWeighInData(
                        docEntry: DataConversionHelper.SafeToInt(lbValDocEntSAP.Text),
                        lineNum: DataConversionHelper.SafeToInt(lblValLineNumSAP.Text),
                        docNum: tbBaseSAP.Text,
                        vendorRef: string.IsNullOrEmpty(_vendorRefK1) ? tbBaseDoc.Text : _vendorRefK1,
                        kontrak: noKontrak,
                        brutoKebun: calc.Bruto[0],
                        taraKebun: taraKebun,
                        nettoKebun: calc.Netto[0],
                        beratMasuk: beratMasukAktual,
                        tiket: tbTiketNum.Text,
                        wbStatus: "WEIGHIN")
                };

                string docNumK1 = !string.IsNullOrEmpty(tbBaseSAP.Text) ? tbBaseSAP.Text : noKontrak;
                var refs = new List<string>
                {
                    !string.IsNullOrEmpty(_vendorRefK1) ? _vendorRefK1 : (!string.IsNullOrEmpty(tbBaseDoc.Text) ? tbBaseDoc.Text : docNumK1)
                };

                for (int i = 0; i < slots.Count; i++)
                {
                    SplitKontrakSlot slot = slots[i];
                    TextBox tbSlot = slot == _k3 ? tbBaseDocK3 : tbBaseDocK2;
                    parts.Add(BuildWeighInData(
                        docEntry: slot.DocEntry,
                        lineNum: slot.LineNum,
                        docNum: slot.DocNum,
                        vendorRef: string.IsNullOrEmpty(slot.VendorRef) ? tbSlot.Text : slot.VendorRef,
                        kontrak: slot.NoKontrak,
                        brutoKebun: calc.Bruto[i + 1],
                        taraKebun: taraKebun,
                        nettoKebun: calc.Netto[i + 1],
                        beratMasuk: beratMasukAktual,
                        tiket: tbTiketNum.Text,
                        wbStatus: "WEIGHIN"));

                    string docNumSlot = !string.IsNullOrEmpty(slot.DocNum) ? slot.DocNum : slot.NoKontrak;
                    refs.Add(!string.IsNullOrEmpty(slot.VendorRef) ? slot.VendorRef : (!string.IsNullOrEmpty(tbSlot.Text) ? tbSlot.Text : docNumSlot));
                }

                string splitNoteMasuk = SplitTicketRowHelper.BuildSplitNote(refs, calc.Netto);
                string existingNoteMasuk = tbNote.Text?.Trim() ?? "";
                string finalNoteMasuk = string.IsNullOrWhiteSpace(existingNoteMasuk) ? splitNoteMasuk : $"{existingNoteMasuk} ({splitNoteMasuk})";
                foreach (var data in parts) data.Note = finalNoteMasuk;
                tbNote.Text = finalNoteMasuk;

                if (_weighInService.SaveSplitWeighIn(parts, out string error))
                {
                    var sukses = new System.Text.StringBuilder("Split Kontrak berhasil disimpan!\n\n");
                    sukses.Append($"Record K1 ({namaK1}): Netto SPB {DocumentLookupItem.FormatKg(calc.Netto[0])} kg\n");
                    for (int i = 0; i < slots.Count; i++)
                        sukses.Append($"Record {slots[i].Label} ({slots[i].DisplayName}): Netto SPB {DocumentLookupItem.FormatKg(calc.Netto[i + 1])} kg\n");
                    sukses.Append($"\nTiket: {tbTiketNum.Text}");
                    MessageBox.Show(sukses.ToString(), "Split Kontrak Berhasil", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }

                MessageBox.Show("Error saat menyimpan Split Kontrak: " + error, "Error - Split Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saat menyimpan Split Kontrak: " + ex.Message, "Error - Split Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private string GetMissingSplitKontrakMessage(out Control focus)
        {
            focus = null;
            if (!_k2.IsSelected)
            {
                focus = tbBaseDocK2;
                return "Mode Split Kontrak aktif.\nHarap isi dan pilih Kontrak Ke-2 terlebih dahulu pada field di atas Nomor Tiket.";
            }
            if (_isKontrakK3 && !_k3.IsSelected)
            {
                focus = tbBaseDocK3;
                return "Mode Split 3 Kontrak aktif.\nHarap isi dan pilih Kontrak Ke-3 terlebih dahulu, atau hilangkan centang \"Split 3 Kontrak\".";
            }
            return "";
        }

        // Aturan split SO belum disepakati; sementara hanya PO
        private bool IsSplitKontrakTersedia()
        {
            string docType = cbDocType.SelectedItem?.ToString() ?? (string.IsNullOrEmpty(cbDocType.Text) ? "PO" : cbDocType.Text);
            return string.Equals(docType, "PO", StringComparison.OrdinalIgnoreCase);
        }

        private void cbSplitKontrak_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSplitKontrak.Checked && !IsSplitKontrakTersedia())
            {
                MessageBox.Show("Split kontrak untuk SO belum tersedia. Pilih satu SO saja, atau hubungi admin.",
                    "Split Kontrak SO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                cbSplitKontrak.Checked = false;
                return;
            }

            _isSplitKontrak = cbSplitKontrak.Checked;
            tbBaseDocK2.Enabled = _isSplitKontrak;
            btnCariKontrakK2.Enabled = _isSplitKontrak;

            if (_isSplitKontrak)
            {
                UpdateSplitControlState();
                UpdateSisaKuotaK1Label();
                UpdateSplitKalkulasiLabel();
                tbBaseDocK2.Focus();
            }
            else
            {
                if (cbKontrakK3 != null && cbKontrakK3.Checked) cbKontrakK3.Checked = false;
                ClearSplitKontrakK2();
                UpdateSplitControlState();
            }
        }

        private void cbKontrakK3_CheckedChanged(object sender, EventArgs e)
        {
            _isKontrakK3 = cbKontrakK3.Checked && _isSplitKontrak;
            if (!_isKontrakK3)
            {
                ClearSplitKontrakK3();
                OnNettoKebunChangedForSplit();
            }
            else
            {
                _isUpdatingSplitInput = true;
                tbNettoSPBK2.Text = "";
                _isUpdatingSplitInput = false;
                UpdateNettoSpbK3();
                tbBaseDocK3.Focus();
            }
            UpdateSplitControlState();
            UpdateSplitKalkulasiLabel();
        }

        private void UpdateSisaKuotaK1Label()
        {
            string sisaStr = DocumentLookupItem.FormatKg(sisaKuotaPO);
            string totalStr = DocumentLookupItem.FormatKg(totalKuotaPO);

            if (sisaKuotaPO <= 0)
            {
                lblSisaKuotaK1Info.Text = $"[PERINGATAN] Sisa K1: {sisaStr} kg (HABIS) | Total: {totalStr} kg";
                lblSisaKuotaK1Info.ForeColor = Color.Red;
            }
            else
            {
                lblSisaKuotaK1Info.Text = $"Sisa K1: {sisaStr} kg | Total: {totalStr} kg";
                lblSisaKuotaK1Info.ForeColor = Color.DarkOrange;
            }
        }

        private void UpdateSplitKalkulasiLabel()
        {
            if (!_isSplitKontrak)
            {
                if (lblInfoSplitKalkulasi != null) lblInfoSplitKalkulasi.Text = "";
                return;
            }

            double taraKebun = ParseWeightInput(tbTaraKebun.Text);
            double nettoKebun = GetTotalNettoKebun();

            if (taraKebun <= 0 && nettoKebun <= 0)
            {
                if (lblInfoSplitKalkulasi != null)
                {
                    lblInfoSplitKalkulasi.Text = "Kalkulasi Split: Isi Data Kebun untuk melihat kalkulasi.";
                    lblInfoSplitKalkulasi.ForeColor = Color.DarkSlateBlue;
                }
                return;
            }

            var calc = WeighbridgeCalculator.CalculateSplitBySPB(nettoKebun, GetNettoSpbTetap(), taraKebun);
            if (!calc.NeedsSplit)
            {
                if (lblInfoSplitKalkulasi != null)
                {
                    lblInfoSplitKalkulasi.Text = $"Kalkulasi Split (Ref SPB): {calc.Error}\nSisa Kuota K1: {DocumentLookupItem.FormatKg(sisaKuotaPO)} kg";
                    lblInfoSplitKalkulasi.ForeColor = Color.Red;
                }
                return;
            }

            bool ok = calc.Netto[0] <= sisaKuotaPO;
            var lines = new List<string>
            {
                $"K1: {DocumentLookupItem.FormatKg(calc.Netto[0])} kg | Sisa kuota {DocumentLookupItem.FormatKg(sisaKuotaPO)} kg" +
                (calc.Netto[0] > sisaKuotaPO ? " [MELEBIHI KUOTA K1!]" : "")
            };

            var slots = new List<SplitKontrakSlot> { _k2 };
            if (_isKontrakK3) slots.Add(_k3);
            for (int i = 0; i < slots.Count; i++)
            {
                SplitKontrakSlot slot = slots[i];
                double netto = calc.Netto[i + 1];
                bool isLast = i == slots.Count - 1;
                string line = $"{slot.Label}{(isLast ? " (sisa)" : "")}: {DocumentLookupItem.FormatKg(netto)} kg";
                if (slot.IsSelected)
                {
                    var val = WeighbridgeCalculator.ValidateK2Quota(netto, slot.SisaKuota);
                    line += $" | Sisa kuota {DocumentLookupItem.FormatKg(slot.SisaKuota)} -> {DocumentLookupItem.FormatKg(val.RemainingQuotaAfterDeduction)} kg";
                    if (!val.IsSufficient)
                    {
                        line += $" [KURANG {DocumentLookupItem.FormatKg(val.Shortage)} kg!]";
                        ok = false;
                    }
                }
                lines.Add(line);
            }

            if (lblInfoSplitKalkulasi != null)
            {
                lblInfoSplitKalkulasi.ForeColor = ok ? Color.DarkSlateBlue : Color.Red;
                lblInfoSplitKalkulasi.Text = string.Join("\n", lines);
            }
        }

        private void ClearSplitKontrakK2()
        {
            _k2.Clear();
            if (tbBaseDocK2 != null) tbBaseDocK2.Text = "";
            if (tbNettoSPBK1 != null) tbNettoSPBK1.Text = "";
            if (tbNettoSPBK2 != null) tbNettoSPBK2.Text = "";
            if (lstSuggestionsK2 != null) { lstSuggestionsK2.Items.Clear(); lstSuggestionsK2.Visible = false; }
            if (lblInfoSplitKalkulasi != null) lblInfoSplitKalkulasi.Text = "";
            if (lblSisaKuotaK1Info != null) lblSisaKuotaK1Info.Text = "";
            ClearSplitKontrakK3();
        }

        private void ClearSplitKontrakK3()
        {
            _k3.Clear();
            _isKontrakK3 = false;
            if (cbKontrakK3 != null && cbKontrakK3.Checked)
            {
                cbKontrakK3.CheckedChanged -= cbKontrakK3_CheckedChanged;
                cbKontrakK3.Checked = false;
                cbKontrakK3.CheckedChanged += cbKontrakK3_CheckedChanged;
            }
            if (tbBaseDocK3 != null)
            {
                _k3.IsSelectingSuggestion = true;
                tbBaseDocK3.Text = "";
                _k3.IsSelectingSuggestion = false;
            }
            if (tbNettoSPBK3 != null) tbNettoSPBK3.Text = "";
            if (lstSuggestionsK3 != null) { lstSuggestionsK3.Items.Clear(); lstSuggestionsK3.Visible = false; }
            UpdateSplitControlState();
        }

        private void tbBaseDocK2_TextChanged(object sender, EventArgs e)
        {
            OnKontrakTextChanged(_k2, tbBaseDocK2, lstSuggestionsK2);
        }

        private void tbBaseDocK2_KeyDown(object sender, KeyEventArgs e)
        {
            OnKontrakTextKeyDown(_k2, lstSuggestionsK2, e);
        }

        private void lstSuggestionsK2_Click(object sender, EventArgs e)
        {
            if (lstSuggestionsK2.SelectedItem is DocumentLookupItem item)
                SelectDocumentItem(_k2, item);
        }

        private void lstSuggestionsK2_KeyDown(object sender, KeyEventArgs e)
        {
            OnSuggestionKeyDown(_k2, tbBaseDocK2, lstSuggestionsK2, e);
        }

        private void btnCariKontrakK2_Click(object sender, EventArgs e)
        {
            OnCariKontrakClick(_k2, tbBaseDocK2, lstSuggestionsK2);
        }

        private void OnKontrakTextChanged(SplitKontrakSlot slot, TextBox tb, ListBox lst)
        {
            if (slot.IsSelectingSuggestion) return;

            string query = tb.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                lst.Visible = false;
                lst.Items.Clear();
                return;
            }

            if (!IsSplitKontrakTersedia()) return;
            if (!cbSplitKontrak.Checked) cbSplitKontrak.Checked = true;
            if (slot == _k3 && cbKontrakK3 != null && !cbKontrakK3.Checked) cbKontrakK3.Checked = true;

            if (_cachedOpenDocuments.Count == 0)
                LoadOpenDocuments(silent: true);

            var results = DocumentLookupFilter.Filter(_cachedOpenDocuments, query);
            lst.Items.Clear();
            if (results.Count > 0)
            {
                foreach (var item in results)
                    lst.Items.Add(item);
                lst.Visible = true;
                lst.BringToFront();
            }
            else
            {
                lst.Visible = false;
            }
        }

        private void OnKontrakTextKeyDown(SplitKontrakSlot slot, ListBox lst, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (lst.Visible && lst.Items.Count > 0)
                {
                    lst.Focus();
                    lst.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (lst.Visible && lst.SelectedItem is DocumentLookupItem selItem)
                {
                    SelectDocumentItem(slot, selItem);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lst.Visible = false;
            }
        }

        private void OnSuggestionKeyDown(SplitKontrakSlot slot, TextBox tb, ListBox lst, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lst.SelectedItem is DocumentLookupItem item)
                    SelectDocumentItem(slot, item);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lst.Visible = false;
                tb.Focus();
            }
        }

        private void OnCariKontrakClick(SplitKontrakSlot slot, TextBox tb, ListBox lst)
        {
            _cachedOpenDocuments.Clear();
            bool loaded = LoadOpenDocuments(silent: false);
            if (loaded && !string.IsNullOrWhiteSpace(tb.Text))
                OnKontrakTextChanged(slot, tb, lst);
            else if (loaded)
            {
                MessageBox.Show($"Cache diperbarui. Ketik nomor kontrak/dokumen {slot.Label} untuk mencari.",
                    $"Refresh {slot.Label} Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SelectDocumentItem(SplitKontrakSlot slot, DocumentLookupItem item)
        {
            if (item == null) return;

            string ke = slot.Label.Substring(1);
            if (item.DocEntry == DataConversionHelper.SafeToInt(lbValDocEntSAP.Text) &&
                item.LineNum == DataConversionHelper.SafeToInt(lblValLineNumSAP.Text))
            {
                MessageBox.Show($"Kontrak Ke-{ke} tidak boleh sama dengan dokumen Kontrak K1. Pilih kontrak/PO lain.",
                    $"Kontrak {slot.Label} Tidak Valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SplitKontrakSlot other = slot == _k2 ? _k3 : _k2;
            if (other.IsSelected && item.DocEntry == other.DocEntry && item.LineNum == other.LineNum)
            {
                MessageBox.Show($"Kontrak Ke-{ke} tidak boleh sama dengan Kontrak {other.Label}. Pilih kontrak/PO lain.",
                    $"Kontrak {slot.Label} Tidak Valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(_itemCodeK1) && !string.IsNullOrEmpty(item.ItemCode) &&
                !string.Equals(item.ItemCode.Trim(), _itemCodeK1.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"Barang pada Kontrak Ke-{ke} ({item.ItemCode}{(string.IsNullOrEmpty(item.ItemName) ? "" : " - " + item.ItemName)}) " +
                    $"berbeda dengan barang Kontrak K1 ({_itemCodeK1}).\n\nPilih PO {slot.Label} dengan barang yang sama.",
                    $"Kontrak {slot.Label} Tidak Valid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            TextBox tb = slot == _k3 ? tbBaseDocK3 : tbBaseDocK2;
            ListBox lst = slot == _k3 ? lstSuggestionsK3 : lstSuggestionsK2;

            slot.IsSelectingSuggestion = true;

            slot.DocEntry = item.DocEntry;
            slot.LineNum = item.LineNum;
            slot.NoKontrak = item.ContractNo;
            slot.DocNum = item.DocNum;
            slot.SisaKuota = item.OpenQty;
            slot.TotalKuota = item.Quantity;
            slot.VendorRef = item.NumAtCard;

            tb.Text = DataConversionHelper.PreferVendorRef(item.NumAtCard, item.DocNum);

            string docType = cbDocType.SelectedItem?.ToString() ?? "PO";
            var fresh = _hanaRepo.GetFreshQuota(docType, item.DocEntry, item.LineNum);
            slot.TotalKuota = fresh.totalQty;

            string freshKontrak = _hanaRepo.GetFreshKontrakNo(docType, item.DocEntry);
            slot.NoKontrak = !string.IsNullOrEmpty(freshKontrak) ? freshKontrak : (!string.IsNullOrEmpty(item.ContractNo) ? item.ContractNo : "");
            slot.SisaKuotaSAP = fresh.openQty;
            slot.PendingNetto = WeighbridgeCalculator.SumPendingNetto(
                GetPendingTicketsForDoc(docType, item.DocNum, slot.NoKontrak, slot.VendorRef));
            slot.SisaKuota = WeighbridgeCalculator.CalculateEffectiveQuota(slot.SisaKuotaSAP, slot.PendingNetto);

            lst.Visible = false;
            lst.Items.Clear();
            slot.IsSelectingSuggestion = false;

            UpdateSplitKalkulasiLabel();

            string info =
                $"Kontrak {slot.Label} dipilih:\n" +
                $"Doc: {item.DocNum}  |  Kontrak: {(string.IsNullOrEmpty(slot.NoKontrak) ? "-" : slot.NoKontrak)}\n" +
                $"Vendor Ref: {(string.IsNullOrEmpty(slot.VendorRef) ? "-" : slot.VendorRef)}\n" +
                $"Kuota {slot.Label}: {DocumentLookupItem.FormatKg(slot.TotalKuota)} kg  |  Sisa {slot.Label} SAP: {DocumentLookupItem.FormatKg(slot.SisaKuotaSAP)} kg\n" +
                (slot.PendingNetto > 0 ? $"Muatan pending WB di {slot.Label}: {DocumentLookupItem.FormatKg(slot.PendingNetto)} kg\n" : "") +
                $"Sisa Kuota Efektif {slot.Label}: {DocumentLookupItem.FormatKg(slot.SisaKuota)} kg";
            MessageBox.Show(info, $"Kontrak {slot.Label} Dipilih", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
