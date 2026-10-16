using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class TimbangKeluarForm
    {
        private bool _isSplitKontrakTicket = false;
        private List<DataRow> _splitRows = new List<DataRow>();
        private List<TicketLookupItem> _cachedTickets = new List<TicketLookupItem>();
        private bool _isSelectingTicketSuggestion = false;

        private void CheckIfSplitTicket(string tiketNo)
        {
            try
            {
                DataTable dt = _hanaRepo.SearchTicketByTiketNum(tiketNo);
                if (dt.Rows.Count >= 2)
                {
                    SetSplitRows(dt);
                    lblValStatusWB.Text = _splitRows[0]["U_SOL_STATUS"].ToString() + SplitStatusSuffix;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CheckIfSplitTicket error: " + ex.Message);
            }
        }

        private bool LoadPendingTickets(bool silent = true)
        {
            try
            {
                _cachedTickets = _hanaRepo.GetPendingTicketLookupList();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadPendingTickets error: " + ex.Message);
                if (!silent)
                {
                    MessageBox.Show("Gagal memuat daftar tiket pending dari SAP:\n" + ex.Message, "Error Load Tiket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return false;
            }
        }

        private void tbTiketNum_TextChanged(object sender, EventArgs e)
        {
            if (_isSelectingTicketSuggestion) return;

            string query = tbTiketNum.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                if (lstTicketSuggestions != null)
                {
                    lstTicketSuggestions.Visible = false;
                    lstTicketSuggestions.Items.Clear();
                }
                return;
            }

            if (_cachedTickets.Count == 0)
            {
                bool loaded = LoadPendingTickets(silent: true);
                if (!loaded) return;
            }

            var results = TicketLookupFilter.Filter(_cachedTickets, query);

            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.Items.Clear();

                if (results.Count > 0)
                {
                    foreach (var item in results)
                    {
                        lstTicketSuggestions.Items.Add(item);
                    }
                    lstTicketSuggestions.BringToFront();
                    lstTicketSuggestions.Visible = true;
                }
                else
                {
                    lstTicketSuggestions.Visible = false;
                }
            }
        }

        private void tbTiketNum_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (lstTicketSuggestions != null && lstTicketSuggestions.Visible && lstTicketSuggestions.Items.Count > 0)
                {
                    lstTicketSuggestions.Focus();
                    lstTicketSuggestions.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (lstTicketSuggestions != null && lstTicketSuggestions.Visible && lstTicketSuggestions.SelectedItem != null)
                {
                    SelectTicketItem((TicketLookupItem)lstTicketSuggestions.SelectedItem);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (lstTicketSuggestions != null && lstTicketSuggestions.Visible && lstTicketSuggestions.Items.Count > 0)
                {
                    SelectTicketItem((TicketLookupItem)lstTicketSuggestions.Items[0]);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else
                {
                    if (lstTicketSuggestions != null) lstTicketSuggestions.Visible = false;
                    btnCariBaseDoc_Click(sender, e);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                if (lstTicketSuggestions != null)
                {
                    lstTicketSuggestions.Visible = false;
                }
                e.Handled = true;
            }
        }

        private void lstTicketSuggestions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lstTicketSuggestions.SelectedItem != null)
                {
                    SelectTicketItem((TicketLookupItem)lstTicketSuggestions.SelectedItem);
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstTicketSuggestions.Visible = false;
                tbTiketNum.Focus();
                e.Handled = true;
            }
        }

        private void lstTicketSuggestions_Click(object sender, EventArgs e)
        {
            if (lstTicketSuggestions.SelectedItem != null)
            {
                SelectTicketItem((TicketLookupItem)lstTicketSuggestions.SelectedItem);
            }
        }

        private void SelectTicketItem(TicketLookupItem item)
        {
            if (item == null) return;

            _isSelectingTicketSuggestion = true;
            tbTiketNum.Text = item.TiketNum;
            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.Visible = false;
                lstTicketSuggestions.Items.Clear();
            }
            _isSelectingTicketSuggestion = false;

            SearchByTiketNum(item.TiketNum);
        }

        private void SetSplitRows(DataTable dt)
        {
            _splitRows = SplitTicketRowHelper.GetOrderedRows(dt);
            _isSplitKontrakTicket = _splitRows.Count >= 2;
        }

        private void ClearSplitRows()
        {
            _isSplitKontrakTicket = false;
            _splitRows = new List<DataRow>();
        }

        private string SplitStatusSuffix => $" [SPLIT KONTRAK {_splitRows.Count} PO]";

        private static string JumlahGRPOText(int count)
        {
            return count == 3 ? "TIGA (3)" : (count == 2 ? "DUA (2)" : count.ToString());
        }

        private bool SyncSplitGRPO()
        {
            if (_splitRows == null || _splitRows.Count < 2)
            {
                MessageBox.Show("Data baris Split Kontrak tidak lengkap.", "Error - Split Kontrak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            double totalNettoWB = DataConversionHelper.SafeToDouble(tbQtyNetto.Text);
            if (totalNettoWB <= 0)
            {
                MessageBox.Show(
                    "Netto WB tidak valid (0 atau kosong).\n" +
                    "Pastikan berat masuk dan berat keluar sudah diisi dari timbangan WB perusahaan.",
                    "Validasi Netto WB", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            int count = _splitRows.Count;
            string lastLabel = "K" + count;
            var nettoKebun = _splitRows.Select(r => DataConversionHelper.SafeToDouble(r["U_SOL_NETTO_KEBUN"])).ToList();

            // Kontrak selain yang terakhir tidak boleh melebihi sisa qty PO terbaru di SAP (truk lain di PO itu bisa sudah GRPO dengan surplus)
            var sisaKuotaSAP = new List<double>();
            for (int i = 0; i < count - 1; i++)
            {
                DataRow row = _splitRows[i];
                var fresh = _hanaRepo.GetFreshQuota("PO",
                    DataConversionHelper.SafeToInt(row["U_SOL_DOC_ENTRY_SAP"]),
                    DataConversionHelper.SafeToInt(row["U_SOL_LINENUM_SAP"]));
                if (fresh.totalQty <= 0)
                {
                    MessageBox.Show(
                        $"Gagal membaca sisa qty PO K{i + 1} ({row["U_SOL_DOCNUM_SAP"]}) dari SAP. Proses dibatalkan, coba lagi.",
                        $"Validasi Kuota PO K{i + 1}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                sisaKuotaSAP.Add(fresh.openQty);
            }

            var split = WeighbridgeCalculator.CalculateSplitGRPONetto(totalNettoWB, nettoKebun, sisaKuotaSAP);

            DataRow lastRow = _splitRows[count - 1];
            int lastDocEntry = DataConversionHelper.SafeToInt(lastRow["U_SOL_DOC_ENTRY_SAP"]);
            int lastLineNum = DataConversionHelper.SafeToInt(lastRow["U_SOL_LINENUM_SAP"]);
            var freshLast = _hanaRepo.GetFreshQuota("PO", lastDocEntry, lastLineNum);
            double nettoLast = split.NettoWB[count - 1];
            if (freshLast.totalQty > 0 && nettoLast > freshLast.openQty + 0.001)
            {
                var kontrakDipakai = _splitRows
                    .Select(r => (DataConversionHelper.SafeToInt(r["U_SOL_DOC_ENTRY_SAP"]), DataConversionHelper.SafeToInt(r["U_SOL_LINENUM_SAP"])))
                    .ToList();
                OverflowChoice choice = ResolveQuotaOverflow(_splitRows[0], $"Kontrak {lastLabel}", lastDocEntry, lastLineNum,
                    lastRow["U_SOL_DOCNUM_SAP"]?.ToString() ?? "", nettoLast, freshLast.openQty, kontrakDipakai);

                if (choice == OverflowChoice.Batal) return false;
                if (choice == OverflowChoice.KontrakLanjutan)
                {
                    if (!RefreshSplitStateFromDatabase(tbTiketNum.Text)) return false;
                    return SyncSplitGRPO();
                }
            }

            double totalSPBKebun = nettoKebun.Sum();
            double selisihSusut = totalNettoWB - totalSPBKebun;
            string susutText = selisihSusut < 0
                ? $"Susut: {Math.Abs(selisihSusut):N0} kg"
                : (selisihSusut > 0 ? $"Surplus: +{selisihSusut:N0} kg" : "Pas (0 kg)");

            double taraKebunK1 = DataConversionHelper.SafeToDouble(_splitRows[0]["U_SOL_TARA_KEBUN"]);
            double taraWB = Math.Min(DataConversionHelper.SafeToDouble(tbQtyMasuk.Text), DataConversionHelper.SafeToDouble(tbQtyKeluar.Text));
            string kebunWarning = WeighbridgeCalculator.IsTaraNettoKebunSwapped(taraKebunK1, totalSPBKebun, taraWB)
                ? $"\n\n[PERINGATAN] Data kebun kemungkinan TERTUKAR saat timbang masuk: Tara Kebun {taraKebunK1:N0} kg, " +
                  $"Total Netto SPB {totalSPBKebun:N0} kg, Tara WB {taraWB:N0} kg. Periksa SPB kebun."
                : "";

            var preview = new StringBuilder();
            preview.Append($"Konfirmasi Pembagian Split GRPO (Tiket {tbTiketNum.Text}):\n\n");
            preview.Append($"  Total Netto SPB Kebun : {totalSPBKebun:N0} kg\n");
            preview.Append($"  Total Netto Real WB   : {totalNettoWB:N0} kg ({susutText})\n\n");
            preview.Append("Alokasi GRPO yang akan dirilis ke SAP:\n");
            for (int i = 0; i < count; i++)
            {
                DataRow row = _splitRows[i];
                string doc = row["U_SOL_DOCNUM_SAP"]?.ToString() ?? "";
                string kon = row["U_SOL_NO_KONTRAK"]?.ToString() ?? "";
                preview.Append($"  {i + 1}. Kontrak K{i + 1} (Doc: {doc} | Kontrak: {(string.IsNullOrEmpty(kon) ? "-" : kon)}):\n");
                preview.Append(nettoKebun[i] > 0
                    ? $"     Netto GRPO: {split.NettoWB[i]:N0} kg (Ref SPB: {nettoKebun[i]:N0} kg)\n"
                    : $"     Netto GRPO: {split.NettoWB[i]:N0} kg (kontrak lanjutan saat timbang keluar, tanpa SPB)\n");
                if (i > 0 && split.LimpahanMasuk[i] > 0)
                    preview.Append($"     [Termasuk limpahan dari K{i}: {split.LimpahanMasuk[i]:N0} kg]\n");
                if (split.LimitedBySAPQuota[i])
                    preview.Append($"     [K{i + 1} dibatasi sisa qty PO K{i + 1} di SAP: {sisaKuotaSAP[i]:N0} kg, kelebihan dialihkan ke kontrak berikutnya]\n");
                preview.Append("\n");
            }
            if (count > 2)
                preview.Append($"  {lastLabel} = Netto Real WB - Netto K1 - Netto K{count - 1} (menampung susut/surplus)\n\n");
            preview.Append($"Lanjutkan rilis {JumlahGRPOText(count)} GRPO ke SAP?");
            preview.Append(kebunWarning);

            if (MessageBox.Show(preview.ToString(), "Konfirmasi Rilis Split GRPO", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes)
            {
                return false;
            }

            var parts = new List<SplitGRPOPart>();
            var refs = new List<string>();
            for (int i = 0; i < count; i++)
            {
                DataRow row = _splitRows[i];
                WeighOutData data = BuildWeighOutData(row, split.NettoWB[i]);
                parts.Add(new SplitGRPOPart { Data = data, Netto = split.NettoWB[i] });

                string vendorRef = row.Table.Columns.Contains("U_SOL_VENDOR_REF") ? row["U_SOL_VENDOR_REF"]?.ToString()?.Trim() : "";
                refs.Add(!string.IsNullOrWhiteSpace(vendorRef) ? vendorRef : data.DocNum);
            }

            string splitNoteKeluar = SplitTicketRowHelper.BuildSplitNote(refs, split.NettoWB);
            string existingNoteKeluar = tbNote.Text?.Trim() ?? "";
            string finalNoteKeluar;
            if (string.IsNullOrWhiteSpace(existingNoteKeluar) ||
                System.Text.RegularExpressions.Regex.IsMatch(existingNoteKeluar, @"^\(?.*SPLIT.*KG\)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                System.Text.RegularExpressions.Regex.IsMatch(existingNoteKeluar, @"^\(?\d+.*SPLIT.*\d+.*\)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                finalNoteKeluar = splitNoteKeluar;
            }
            else
            {
                finalNoteKeluar = $"{existingNoteKeluar} ({splitNoteKeluar})";
            }
            foreach (var part in parts) part.Data.Note = finalNoteKeluar;
            tbNote.Text = finalNoteKeluar;

            if (_weighOutService.SyncSplitGRPO(parts, out List<int> docEntries, out string error))
            {
                var msg = new StringBuilder();
                msg.Append($"Berhasil Merilis Split GRPO untuk Tiket {tbTiketNum.Text}!\n\n");
                for (int i = 0; i < count; i++)
                {
                    int docEntry = i < docEntries.Count ? docEntries[i] : 0;
                    WeighOutData data = parts[i].Data;
                    msg.Append($"{i + 1}. GRPO Kontrak {i + 1} (PO: {data.DocNum} | Kontrak: {data.NoKontrak}):\n");
                    if (docEntry > 0)
                        msg.Append($"   No. GRPO : {_hanaRepo.GetDocNumByEntry("OPDN", docEntry.ToString())} (DocEntry: {docEntry})\n");
                    else
                        msg.Append("   Tidak dibuat GRPO (netto 0 kg)\n");
                    msg.Append($"   Netto WB : {parts[i].Netto:N0} kg\n\n");
                }

                msg.Append("Status Weighbridge: SYNCSAP.");
                if (string.IsNullOrEmpty(error))
                    MessageBox.Show(msg.ToString(), "Sukses Rilis Split GRPO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(msg + "\n\nPERINGATAN:\n" + error, "Peringatan - Split GRPO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }

            MessageBox.Show("Error saat rilis Split GRPO: " + error, "Error - Split GRPO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}
