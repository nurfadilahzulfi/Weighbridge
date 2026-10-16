using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class TimbangKeluarForm
    {
        private const string TitleSuffix = " - PT Tanimas Resources International • © 2026 Nur Fadilah Zulfi | Diah Fika Satrya";

        private enum OverflowChoice { Batal, TetapGRPO, KontrakLanjutan }
        private OverflowChoice ResolveQuotaOverflow(DataRow templateRow, string label, int docEntry, int lineNum, string docNum,
                                                    double netto, double sisaPO, IList<(int DocEntry, int LineNum)> kontrakDipakai)
        {
            double kelebihan = Math.Round(netto - sisaPO, 2);
            while (true)
            {
                OverflowChoice choice;
                using (var dlg = new QuotaOverflowDialog(label, docNum, netto, sisaPO, kelebihan))
                {
                    dlg.ShowDialog(this);
                    choice = dlg.Choice;
                }
                if (choice != OverflowChoice.KontrakLanjutan) return choice;

                List<DocumentLookupItem> kandidat;
                try
                {
                    kandidat = GetKandidatKontrakLanjutan(docEntry, lineNum, kontrakDipakai);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal membaca daftar PO terbuka dari SAP:\n" + ex.Message, "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                if (kandidat.Count == 0)
                {
                    MessageBox.Show(
                        "Tidak ada PO terbuka lain untuk vendor dan barang yang sama.\n\n" +
                        "Pilih \"Tetap GRPO di PO ini\" atau Batal, lalu hubungi bagian pembelian.",
                        "Kontrak Lanjutan Tidak Ada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                DocumentLookupItem dipilih;
                using (var picker = new KontrakLanjutanDialog(kandidat, kelebihan))
                {
                    if (picker.ShowDialog(this) != DialogResult.OK || picker.Selected == null) continue;
                    dipilih = picker.Selected;
                }

                if (!AddKontrakLanjutan(templateRow, dipilih, out string error))
                {
                    MessageBox.Show("Gagal menambahkan kontrak lanjutan:\n" + error, "Error - Kontrak Lanjutan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return OverflowChoice.Batal;
                }
                return OverflowChoice.KontrakLanjutan;
            }
        }

        private List<DocumentLookupItem> GetKandidatKontrakLanjutan(int docEntry, int lineNum, IList<(int DocEntry, int LineNum)> kontrakDipakai)
        {
            var (cardCode, itemCode) = _hanaRepo.GetPOVendorAndItem(docEntry, lineNum);
            var openDocs = new DocumentLookupService(_hanaRepo).GetOpenDocuments("PO", forceRefresh: true);

            return openDocs
                .Where(d => d.OpenQty > 0)
                .Where(d => string.IsNullOrEmpty(cardCode) || string.Equals(d.CardCode, cardCode, StringComparison.OrdinalIgnoreCase))
                .Where(d => string.IsNullOrEmpty(itemCode) || string.Equals(d.ItemCode, itemCode, StringComparison.OrdinalIgnoreCase))
                .Where(d => !kontrakDipakai.Any(k => k.DocEntry == d.DocEntry && k.LineNum == d.LineNum))
                .OrderByDescending(d => d.OpenQty)
                .ToList();
        }

        private bool AddKontrakLanjutan(DataRow row, DocumentLookupItem item, out string error)
        {
            string Col(string name) => row.Table.Columns.Contains(name) && row[name] != DBNull.Value ? row[name]?.ToString() ?? "" : "";
            DateTime? ColDate(string name) =>
                row.Table.Columns.Contains(name) && row[name] is DateTime d ? d : (DateTime?)null;

            double taraKebun = DataConversionHelper.SafeToDouble(Col("U_SOL_TARA_KEBUN"));
            string freshKontrak = _hanaRepo.GetFreshKontrakNo("PO", item.DocEntry);
            DateTime? tanggalMasuk = ColDate("U_SOL_TANGGAL_MASUK");

            var data = new WeighInData
            {
                DocType = "PO",
                DocNum = item.DocNum,
                VendorRef = item.NumAtCard ?? "",
                DocEntry = item.DocEntry,
                LineNum = item.LineNum,
                TiketNum = Col("U_SOL_TIKET_WB"),
                Commodity = Col("U_SOL_COMMODITY"),
                NoKontrak = !string.IsNullOrEmpty(freshKontrak) ? freshKontrak : (item.ContractNo ?? ""),
                Nopol = Col("U_SOL_NOPOL"),
                NamaSupir = Col("U_SOL_NAMA_SUPIR"),
                NoSimSupir = Col("U_SOL_NO_SIM_SUPIR"),
                JenisTruck = Col("U_SOL_JENIS_TRUCK"),
                BeratMasuk = DataConversionHelper.SafeToDouble(Col("U_SOL_BERAT_MASUK")),
                BrutoKebun = taraKebun,
                TaraKebun = taraKebun,
                NettoKebun = 0,
                FfaKebun = Col("U_SOL_FFA_KEBUN"),
                MoistKebun = Col("U_SOL_MOIST_KEBUN"),
                LabFfa = Col("U_SOL_LAB_FFA"),
                LabMoist = Col("U_SOL_LAB_MOIST"),
                LabImp = Col("U_SOL_LAB_IMP"),
                LabDobi = Col("U_SOL_LAB_DOBI"),
                LabCarotine = Col("U_SOL_LAB_CAROTINE"),
                KodeDokumenPabean = Col("U_SOL_KODE_DOKUMEN"),
                NoDokumenPabean = Col("U_SOL_NO_DOKUMEN") != "" ? Col("U_SOL_NO_DOKUMEN") : Col("U_SOL_NO_DOKUMEN_PABEAN"),
                TanggalDokumenPabean = ColDate("U_SOL_TANGGAL_DOKUMEN"),
                NomorSpb = Col("U_SOL_NOMOR_SPB"),
                NomorAju = Col("U_SOL_NOMOR_AJU"),
                SealNo = Col("U_SOL_SEAL_NO"),
                DoNo = Col("U_SOL_DO_NO"),
                Container1No = Col("U_SOL_CONTAINER_1_NO"),
                Container2No = Col("U_SOL_CONTAINER_2_NO"),
                Note = Col("U_SOL_NOTE"),
                Username = Col("U_SOL_USER"),
                Status = "WEIGHIN",
                TransporterInt = Col("U_SOL_TRANSPORTER_INT"),
                TransporterEks = Col("U_SOL_TRANSPORTER_EKS"),
                TanggalMasuk = tanggalMasuk,
                WaktuMasuk = DataConversionHelper.ParseWaktuMasuk(row.Table.Columns.Contains("U_SOL_WAKTU_MASUK") ? row["U_SOL_WAKTU_MASUK"] : null, tanggalMasuk)
            };

            return new WeighInService(_hanaRepo, _sapRepo).SaveWeighIn(data, out error);
        }
        private sealed class QuotaOverflowDialog : Form
        {
            public OverflowChoice Choice { get; private set; } = OverflowChoice.Batal;

            public QuotaOverflowDialog(string label, string docNum, double netto, double sisaPO, double kelebihan)
            {
                Text = "Kuota PO Tidak Cukup" + TitleSuffix;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(560, 230);
                Font = new Font("Segoe UI", 9F);

                var lbl = new Label
                {
                    Location = new Point(16, 14),
                    Size = new Size(528, 140),
                    Text =
                        $"Netto untuk {label} (PO {docNum}) = {DocumentLookupItem.FormatKg(netto)} kg, " +
                        $"tetapi sisa qty PO di SAP hanya {DocumentLookupItem.FormatKg(Math.Max(0, sisaPO))} kg.\n" +
                        $"Kelebihan: {DocumentLookupItem.FormatKg(kelebihan)} kg.\n\n" +
                        "Pilih cara menampung kelebihan:\n" +
                        "•  Lanjut ke kontrak lain: kelebihan masuk ke PO lain (vendor dan barang sama), tiket diproses sebagai split kontrak.\n" +
                        "•  Tetap GRPO di PO ini: seluruh netto masuk ke PO ini (kelebihan terima)."
                };

                var btnLanjut = new Button { Text = "Lanjut ke kontrak lain", Location = new Point(16, 176), Size = new Size(180, 34) };
                var btnTetap = new Button { Text = "Tetap GRPO di PO ini", Location = new Point(206, 176), Size = new Size(180, 34) };
                var btnBatal = new Button { Text = "Batal", Location = new Point(444, 176), Size = new Size(100, 34), DialogResult = DialogResult.Cancel };

                btnLanjut.Click += (s, e) => { Choice = OverflowChoice.KontrakLanjutan; DialogResult = DialogResult.OK; };
                btnTetap.Click += (s, e) =>
                {
                    var konfirmasi = MessageBox.Show(
                        $"Seluruh netto {DocumentLookupItem.FormatKg(netto)} kg akan di-GRPO ke PO {docNum} " +
                        $"(kelebihan terima {DocumentLookupItem.FormatKg(kelebihan)} kg).\n\nLanjutkan?",
                        "Konfirmasi Kelebihan Terima", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (konfirmasi != DialogResult.Yes) return;
                    Choice = OverflowChoice.TetapGRPO;
                    DialogResult = DialogResult.OK;
                };

                Controls.AddRange(new Control[] { lbl, btnLanjut, btnTetap, btnBatal });
                AcceptButton = btnLanjut;
                CancelButton = btnBatal;
            }
        }

        private sealed class KontrakLanjutanDialog : Form
        {
            private readonly List<DocumentLookupItem> _all;
            private readonly TextBox _tbCari;
            private readonly ListBox _lst;

            public DocumentLookupItem Selected { get; private set; }

            public KontrakLanjutanDialog(List<DocumentLookupItem> kandidat, double kelebihan)
            {
                _all = kandidat;
                Text = "Pilih Kontrak Lanjutan" + TitleSuffix;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(760, 380);
                Font = new Font("Segoe UI", 9F);

                var lblInfo = new Label
                {
                    Location = new Point(16, 12),
                    Size = new Size(728, 34),
                    Text = $"Kelebihan {DocumentLookupItem.FormatKg(kelebihan)} kg akan masuk ke PO yang dipilih (vendor dan barang sama). " +
                           "Ketik untuk mencari nomor PO / Ref vendor."
                };
                _tbCari = new TextBox { Location = new Point(16, 50), Size = new Size(728, 23) };
                _lst = new ListBox
                {
                    Location = new Point(16, 80),
                    Size = new Size(728, 244),
                    HorizontalScrollbar = true,
                    DisplayMember = nameof(DocumentLookupItem.DisplayText)
                };
                var btnPilih = new Button { Text = "Pilih", Location = new Point(538, 334), Size = new Size(100, 32) };
                var btnBatal = new Button { Text = "Batal", Location = new Point(644, 334), Size = new Size(100, 32), DialogResult = DialogResult.Cancel };

                _tbCari.TextChanged += (s, e) => Isi(_tbCari.Text);
                _lst.DoubleClick += (s, e) => Pilih();
                btnPilih.Click += (s, e) => Pilih();

                Controls.AddRange(new Control[] { lblInfo, _tbCari, _lst, btnPilih, btnBatal });
                AcceptButton = btnPilih;
                CancelButton = btnBatal;
                Isi("");
            }

            private void Isi(string query)
            {
                var items = string.IsNullOrWhiteSpace(query) ? _all : DocumentLookupFilter.Filter(_all, query);
                _lst.BeginUpdate();
                _lst.Items.Clear();
                foreach (var item in items) _lst.Items.Add(item);
                _lst.EndUpdate();
                if (_lst.Items.Count > 0) _lst.SelectedIndex = 0;
            }

            private void Pilih()
            {
                if (!(_lst.SelectedItem is DocumentLookupItem item))
                {
                    MessageBox.Show("Pilih salah satu PO.", "Kontrak Lanjutan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Selected = item;
                DialogResult = DialogResult.OK;
            }
        }
    }
}
