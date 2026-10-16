using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge
{
    public partial class ReportForm : Form
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly IExcelExportService _excelExportService;
        private DataTable _currentData = new DataTable();

        private ListBox lstTicketSuggestions;
        private List<TicketLookupItem> _cachedTickets = new List<TicketLookupItem>();
        private bool _isSelectingTicketSuggestion = false;

        public ReportForm() : this(new HanaRepository(), new ExcelExportService())
        {
        }

        public ReportForm(IHanaRepository hanaRepo, IExcelExportService excelExportService)
        {
            InitializeComponent();
            _hanaRepo = hanaRepo ?? new HanaRepository();
            _excelExportService = excelExportService ?? new ExcelExportService();
            InitializeTicketSuggestions();
        }

        private void ReportForm_Load(object sender, EventArgs e)
        {
            LoadCommodityList();

            dtFrom.ShowCheckBox = true;
            dtFrom.Checked = false;

            dtTo.ShowCheckBox = true;
            dtTo.Checked = false;

            MinimumSize = Size;
            Weighbridge.Core.Helpers.WatermarkHelper.Apply(this);
            Weighbridge.Core.Helpers.WatermarkHelper.ApplyToGrid(dvgReportData);

            System.Threading.Tasks.Task.Run(() => LoadTicketLookup());
        }

        private void InitializeTicketSuggestions()
        {
            lstTicketSuggestions = new ListBox
            {
                Location = new Point(tbNoTiket.Left, tbNoTiket.Bottom + 2),
                Size = new Size(560, 160),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                HorizontalScrollbar = true,
                Visible = false
            };
            lstTicketSuggestions.Click += lstTicketSuggestions_Click;
            lstTicketSuggestions.KeyDown += lstTicketSuggestions_KeyDown;
            Controls.Add(lstTicketSuggestions);
            lstTicketSuggestions.BringToFront();

            tbNoTiket.TextChanged += tbNoTiket_TextChanged;
            tbNoTiket.KeyDown += tbNoTiket_KeyDown;
        }

        private bool LoadTicketLookup()
        {
            try
            {
                _cachedTickets = _hanaRepo.GetPendingTicketLookupList();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadTicketLookup error: " + ex.Message);
                return false;
            }
        }

        private void tbNoTiket_TextChanged(object sender, EventArgs e)
        {
            if (_isSelectingTicketSuggestion) return;

            string query = tbNoTiket.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                lstTicketSuggestions.Visible = false;
                lstTicketSuggestions.Items.Clear();
                return;
            }

            if ((_cachedTickets == null || _cachedTickets.Count == 0) && !LoadTicketLookup()) return;

            var results = TicketLookupFilter.Filter(_cachedTickets, query);
            lstTicketSuggestions.Items.Clear();
            if (results != null && results.Count > 0)
            {
                foreach (var item in results)
                    lstTicketSuggestions.Items.Add(item);
                lstTicketSuggestions.BringToFront();
                lstTicketSuggestions.Visible = true;
            }
            else
            {
                lstTicketSuggestions.Visible = false;
            }
        }

        private void tbNoTiket_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                if (lstTicketSuggestions.Visible && lstTicketSuggestions.Items.Count > 0)
                {
                    lstTicketSuggestions.Focus();
                    lstTicketSuggestions.SelectedIndex = 0;
                    e.Handled = true;
                }
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (lstTicketSuggestions.Visible && lstTicketSuggestions.Items.Count > 0)
                {
                    var item = lstTicketSuggestions.SelectedItem ?? lstTicketSuggestions.Items[0];
                    SelectTicketItem((TicketLookupItem)item);
                }
                else
                {
                    lstTicketSuggestions.Visible = false;
                    btnSearchReport_Click(sender, e);
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstTicketSuggestions.Visible = false;
                e.Handled = true;
            }
        }

        private void lstTicketSuggestions_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && lstTicketSuggestions.SelectedItem != null)
            {
                SelectTicketItem((TicketLookupItem)lstTicketSuggestions.SelectedItem);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                lstTicketSuggestions.Visible = false;
                tbNoTiket.Focus();
                e.Handled = true;
            }
        }

        private void lstTicketSuggestions_Click(object sender, EventArgs e)
        {
            if (lstTicketSuggestions.SelectedItem != null)
                SelectTicketItem((TicketLookupItem)lstTicketSuggestions.SelectedItem);
        }

        private void SelectTicketItem(TicketLookupItem item)
        {
            if (item == null) return;

            _isSelectingTicketSuggestion = true;
            tbNoTiket.Text = item.TiketNum;
            lstTicketSuggestions.Visible = false;
            lstTicketSuggestions.Items.Clear();
            _isSelectingTicketSuggestion = false;

            btnSearchReport_Click(this, EventArgs.Empty);
        }

        private void LoadCommodityList()
        {
            cmbCommodity.Items.Clear();
            var commodities = _hanaRepo.GetCommodityList();
            foreach (var item in commodities)
            {
                cmbCommodity.Items.Add(item);
            }
        }

        private void btnSearchReport_Click(object sender, EventArgs e)
        {
            string noTiket = tbNoTiket.Text;
            string noPolisi = tbNopol.Text;
            string commodity = cmbCommodity.Text;
            string fromDate = dtFrom.Checked ? dtFrom.Value.ToString("yyyyMMdd") : "";
            string toDate = dtTo.Checked ? dtTo.Value.ToString("yyyyMMdd") : "";

            try
            {
                Cursor = Cursors.WaitCursor;
                _currentData = _hanaRepo.GetReportData(noTiket, noPolisi, commodity, fromDate, toDate);
                Weighbridge.Core.Helpers.ReportDataHelper.ApplySplitBrutoPerKontrak(_currentData);

                dvgReportData.DataSource = _currentData;
                dvgReportData.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dvgReportData.ReadOnly = true;
                dvgReportData.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"HANA connection error:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_currentData == null || _currentData.Rows.Count == 0)
            {
                MessageBox.Show("No data to export. Load data first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Save Excel File",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"ReportWeighbridge{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = "xlsx"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                _excelExportService.ExportDataTableToExcel(_currentData, dlg.FileName);

                if (MessageBox.Show("File saved. Open it now?", "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
