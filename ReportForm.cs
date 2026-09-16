using OfficeOpenXml;
using OfficeOpenXml.Style;
using Sap.Data.Hana;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Weighbridge.Connection;

namespace Weighbridge
{
    public partial class ReportForm : Form
    {
        HANAConnetion hanaConn = new HANAConnetion();

        private DataTable _currentData = new DataTable();

        public ReportForm()
        {
            InitializeComponent();
        }

        private void ReportForm_Load(object sender, EventArgs e)
        {
            loadDataCommodity();

            dtFrom.ShowCheckBox = true;
            dtFrom.Checked = false;

            dtTo.ShowCheckBox = true;
            dtTo.Checked = false;
        }

        private DataTable loadReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate)
        {
            DataTable dt = new DataTable();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = $"CALL SOL_SP_ADDON_WB_REPORT('{noTiket}', '{noPolisi}', '{commodity}', '{fromDate}', '{toDate}')";

                using (var command = new HanaCommand(queryString, connection))
                {
                    HanaDataAdapter adapter = new HanaDataAdapter(command);
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        private void btnSearchReport_Click(object sender, EventArgs e)
        {
            string noTiket = tbNoTiket.Text;
            string noPolisi = tbNopol.Text;
            string commodity = cmbCommodity.Text;
            string fromDate = dtTo.Checked ? dtFrom.Value.ToString("yyyyMMdd") : "";
            string toDate = dtTo.Checked ? dtTo.Value.ToString("yyyyMMdd") : "";

            try
            {
                Cursor = Cursors.WaitCursor;
                _currentData = loadReportData(noTiket, noPolisi, commodity, fromDate, toDate);

                dvgReportData.DataSource = _currentData;

                // Auto-size columns for readability
                dvgReportData.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                dvgReportData.ReadOnly = true;
                dvgReportData.AllowUserToAddRows = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"HANA connection error:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("No data to export. Load data first.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                ExportToExcel(_currentData, dlg.FileName);

                if (MessageBox.Show("File saved. Open it now?",
                        "Success", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(dlg.FileName)
                        { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error:\n\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void loadDataCommodity()
        {
            cmbCommodity.Items.Clear();

            HANAConnetion hanaConn = new HANAConnetion();
            string connectionString = hanaConn.HanaConnectionStrings(ConfigManager.sboConfig);

            var connection = new HanaConnection(connectionString);

            using (connection)
            {
                connection.Open();

                string queryString = "CALL SOL_SP_ADDON_WB_COMMODITY_LIST()";

                using (var command = new HanaCommand(queryString, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cmbCommodity.Items.Add(reader["COMMODITY"]);
                        }
                    }
                }

                connection.Close();
            }
        }

        private static void ExportToExcel(DataTable dt, string filePath)
        {
            // Required for EPPlus 5+
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Weighbridge Data");

            // Header row
            for (int col = 0; col < dt.Columns.Count; col++)
            {
                var cell = ws.Cells[1, col + 1];
                cell.Value = dt.Columns[col].ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(
                    System.Drawing.Color.FromArgb(68, 114, 196));
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.HorizontalAlignment =
                    ExcelHorizontalAlignment.Center;
            }

            // Data rows
            for (int row = 0; row < dt.Rows.Count; row++)
            {
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    var cell = ws.Cells[row + 2, col + 1];
                    var value = dt.Rows[row][col];

                    if (value == DBNull.Value)
                        cell.Value = string.Empty;
                    else if (value is DateTime dt2)
                        cell.Value = dt2;
                    else if (value is decimal || value is double ||
                             value is float || value is int ||
                             value is long)
                        cell.Value = Convert.ToDouble(value);
                    else
                        cell.Value = value.ToString();

                    // Zebra striping
                    if (row % 2 == 1)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(
                            System.Drawing.Color.FromArgb(242, 242, 242));
                    }
                }
            }

            // Freeze header, auto-filter, auto-fit
            ws.View.FreezePanes(2, 1);
            ws.Cells[ws.Dimension.Address].AutoFilter = true;
            ws.Cells.AutoFitColumns();

            // Footer
            int footerRow = dt.Rows.Count + 3;
            ws.Cells[footerRow, 1].Value =
                $"Total rows: {dt.Rows.Count}  |  Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            ws.Cells[footerRow, 1].Style.Font.Italic = true;

            package.SaveAs(new FileInfo(filePath));
        }

    }
}
