using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class ReprintTiket : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;
        private readonly IPrintService _printService;
        private List<TicketLookupItem> _cachedTickets = new List<TicketLookupItem>();
        private bool _isSelectingTicketSuggestion = false;

        public ReprintTiket() : this(new HanaRepository(), new SapRepository(), new PrintService())
        {
        }

        public ReprintTiket(IHanaRepository hanaRepo, ISapRepository sapRepo, IPrintService printService)
        {
            _hanaRepo = hanaRepo ?? new HanaRepository();
            _sapRepo = sapRepo ?? new SapRepository();
            _printService = printService ?? new PrintService(_hanaRepo);
            InitializeComponent();
        }

        private void ReprintTiket_Load(object sender, EventArgs e)
        {
            lblUsername.Text = SessionUser.username;
            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.BringToFront();
            }
            ResponsiveLayoutHelper.Attach(this);
            WatermarkHelper.Apply(this);
            System.Threading.Tasks.Task.Run(() => LoadTicketLookup(silent: true));
        }

        private bool LoadTicketLookup(bool silent = true)
        {
            try
            {
                _cachedTickets = _hanaRepo.GetPendingTicketLookupList();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadTicketLookup error: " + ex.Message);
                if (!silent)
                {
                    MessageBox.Show("Gagal memuat daftar tiket dari SAP:\n" + ex.Message, "Error Load Tiket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            if (_cachedTickets == null || _cachedTickets.Count == 0)
            {
                bool loaded = LoadTicketLookup(silent: true);
                if (!loaded) return;
            }

            var results = TicketLookupFilter.Filter(_cachedTickets, query);

            if (lstTicketSuggestions != null)
            {
                lstTicketSuggestions.Items.Clear();

                if (results != null && results.Count > 0)
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
                    string noTiket = tbTiketNum.Text.Trim();
                    if (!string.IsNullOrEmpty(noTiket))
                    {
                        SearchByTiketNum(noTiket, showSuccessMsg: true);
                    }
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

            SearchByTiketNum(item.TiketNum, showSuccessMsg: false);
        }

        public void SearchByTiketNum(string noTiket, bool showSuccessMsg = false)
        {
            if (string.IsNullOrWhiteSpace(noTiket)) return;

            DataTable result = _hanaRepo.SearchTicketByTiketNum(noTiket.Trim());
            if (result == null || result.Rows.Count == 0)
            {
                MessageBox.Show("Nomor Tiket tidak ditemukan.", "Not Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (showSuccessMsg)
            {
                MessageBox.Show("Nomor Tiket ditemukan.", "Found - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            DataRow first = result.Rows[0];
            if (result.Rows.Count >= 2) SplitTicketRowHelper.GetK1K2(result, out first, out _);
            fillFormFromDataRow(first);
        }

        // Tiket split: tampilkan kebun dan netto WB satu truk utuh (semua kontrak K1..K3), sama seperti tiket yang dicetak
        private void ShowSplitTotals(string tiketNum)
        {
            if (string.IsNullOrWhiteSpace(tiketNum)) return;
            DataTable rows;
            try { rows = _hanaRepo.SearchTicketByTiketNum(tiketNum.Trim()); }
            catch { return; }
            if (rows == null || rows.Rows.Count < 2) return;

            List<DataRow> ordered = SplitTicketRowHelper.GetOrderedRows(rows);
            var kebun = SplitTicketRowHelper.CombineKebun(
                ordered.Select(r => DataConversionHelper.SafeToDouble(r["U_SOL_NETTO_KEBUN"])),
                DataConversionHelper.SafeToDouble(ordered[0]["U_SOL_TARA_KEBUN"]));

            tbBrutoKebun.Text = kebun.Bruto.ToString("0.##");
            tbTaraKebun.Text = kebun.Tara.ToString("0.##");
            tbNettoKebun.Text = kebun.Netto.ToString("0.##");

            if (rows.Columns.Contains("U_SOL_BERAT_NETTO"))
            {
                double nettoWB = ordered.Sum(r => DataConversionHelper.SafeToDouble(r["U_SOL_BERAT_NETTO"]));
                tbQtyNetto.Text = nettoWB.ToString("0.##");
            }
        }

        private void tbBaseDoc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnCariBaseDoc_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void btnRefreshData_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Apakah anda yakin untuk Refresh Data?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                ClearAllField();
                if (lstTicketSuggestions != null)
                {
                    lstTicketSuggestions.Visible = false;
                    lstTicketSuggestions.Items.Clear();
                }
                btnCariBaseDoc.Visible = true;
                lblStatusWB.Visible = false;
                lblValStatusWB.Visible = false;
                tbTiketNum.Enabled = true;
                System.Threading.Tasks.Task.Run(() => LoadTicketLookup(silent: true));
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
                SearchByTiketNum(noTiket, showSuccessMsg: true);
            }
        }

        private void btnWBReprint_Click(object sender, EventArgs e)
        {
            string tiketNumber = tbTiketNum.Text.Trim();
            if (string.IsNullOrEmpty(tiketNumber))
            {
                MessageBox.Show("Nomor tiket tidak valid.", "Error - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Tiket split punya 2 record (K1 & K2): semua harus REPRINT=Y agar SP cetak mengisi data lengkap
            foreach (string code in _hanaRepo.GetWBCodesByTiket(tiketNumber))
            {
                if (!_sapRepo.SetUDOReprintFlag(code, out string sapError))
                {
                    MessageBox.Show($"Peringatan update status reprint SAP ({code}): " + sapError, "Warning - Weighbridge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (!_printService.PrintTicket(tiketNumber, TicketPrintType.Reprint, out string printError))
            {
                MessageBox.Show($"Failed to print ticket:\n{printError}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show($"Ticket {tiketNumber} sent to printer.", "Print Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void fillFormFromDataRow(DataRow row)
        {
            string GetVal(string col) => row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col].ToString() : "";

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
            tbBaseDoc.Text = DataConversionHelper.PreferVendorRef(vendorRef, docNumSap);
            tbBaseSAP.Text = docNumSap;
            tbTiketNum.Text = GetVal("U_SOL_TIKET_WB");
            lbValDocEntSAP.Text = GetVal("U_SOL_DOC_ENTRY_SAP");
            lblValLineNumSAP.Text = GetVal("U_SOL_LINENUM_SAP");
            lblValStatusWB.Text = GetVal("U_SOL_STATUS");

            tbNopol.Text = GetVal("U_SOL_NOPOL");
            tbNamaSupir.Text = GetVal("U_SOL_NAMA_SUPIR");
            tbSIMSupir.Text = GetVal("U_SOL_NO_SIM_SUPIR");
            tbJnsTruck.Text = GetVal("U_SOL_JENIS_TRUCK");
            tbBrutoKebun.Text = GetVal("U_SOL_BRUTO_KEBUN");
            tbTaraKebun.Text = GetVal("U_SOL_TARA_KEBUN");
            tbNettoKebun.Text = GetVal("U_SOL_NETTO_KEBUN");
            tbFFAKebun.Text = GetVal("U_SOL_FFA_KEBUN");
            tbMOISTKebun.Text = GetVal("U_SOL_MOIST_KEBUN");

            tbFFA.Text = GetVal("U_SOL_LAB_FFA");
            tbMOIST.Text = GetVal("U_SOL_LAB_MOIST");
            tbDOBI.Text = GetVal("U_SOL_LAB_DOBI");
            tbIMP.Text = GetVal("U_SOL_LAB_IMP");
            tbCAROTINE.Text = GetVal("U_SOL_LAB_CAROTINE");

            tbCommodity.Text = GetVal("U_SOL_COMMODITY");
            tbSealNo.Text = GetVal("U_SOL_SEAL_NO");
            tbDONo.Text = GetVal("U_SOL_DO_NO");
            tbNote.Text = GetVal("U_SOL_NOTE");
            tbKodeDokPabean.Text = GetVal("U_SOL_KODE_DOKUMEN");
            tbTangkiNo.Text = GetVal("U_SOL_NOMOR_TANGKI");
            tbNoDokPabean.Text = GetVal("U_SOL_NO_DOKUMEN_PABEAN");
            tbNomorSPB.Text = GetVal("U_SOL_NOMOR_SPB");
            tbNomorAju.Text = GetVal("U_SOL_NOMOR_AJU");
            tbContNo1.Text = GetVal("U_SOL_CONTAINER_1_NO");
            tbContNo2.Text = GetVal("U_SOL_CONTAINER_2_NO");

            tbQtyMasuk.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_MASUK") ? row["U_SOL_BERAT_MASUK"] : null).ToString("0.##");
            tbQtyKeluar.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_KELUAR") ? row["U_SOL_BERAT_KELUAR"] : null).ToString("0.##");
            tbQtyNetto.Text = DataConversionHelper.SafeToDouble(row.Table.Columns.Contains("U_SOL_BERAT_NETTO") ? row["U_SOL_BERAT_NETTO"] : null).ToString("0.##");

            lblValTanggalMasuk.Text = GetVal("U_SOL_TANGGAL_MASUK");
            lblValWaktuMasuk.Text = GetVal("U_SOL_WAKTU_MASUK");
            tbTransporterInt.Text = GetVal("U_SOL_TRANSPORTER_INT");
            tbTransporterEks.Text = GetVal("U_SOL_TRANSPORTER_EKS");

            btnCariBaseDoc.Visible = false;
            btnRefreshData.Visible = true;
            lblStatusWB.Visible = true;
            lblValStatusWB.Visible = true;
            tbTiketNum.Enabled = false;
            btnWBReprint.Enabled = true;

            ShowSplitTotals(GetVal("U_SOL_TIKET_WB"));
        }

        private void ClearAllField()
        {
            lbValDocEntSAP.Text = string.Empty;
            lblValLineNumSAP.Text = string.Empty;
            cbDocType.SelectedIndex = -1;
            tbBaseDoc.Text = string.Empty;
            tbBaseSAP.Text = string.Empty;
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
            tbTransporterInt.Text = string.Empty;
            tbTransporterEks.Text = string.Empty;
        }
    }
}
