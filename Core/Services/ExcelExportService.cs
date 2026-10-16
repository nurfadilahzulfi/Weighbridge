using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Data;
using System.Drawing;
using System.IO;

namespace Weighbridge.Core.Services
{
    public interface IExcelExportService
    {
        void ExportDataTableToExcel(DataTable dt, string filePath, string sheetName = "Weighbridge Data");
    }

    public class ExcelExportService : IExcelExportService
    {
        public void ExportDataTableToExcel(DataTable dt, string filePath, string sheetName = "Weighbridge Data")
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);

                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    var cell = ws.Cells[1, col + 1];
                    cell.Value = dt.Columns[col].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                for (int row = 0; row < dt.Rows.Count; row++)
                {
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        var cell = ws.Cells[row + 2, col + 1];
                        var value = dt.Rows[row][col];

                        if (value == DBNull.Value || value == null)
                        {
                            cell.Value = string.Empty;
                        }
                        else if (value is DateTime dtVal)
                        {
                            cell.Value = dtVal;
                            cell.Style.Numberformat.Format = "yyyy-MM-dd HH:mm:ss";
                        }
                        else if (value is decimal || value is double || value is float || value is int || value is long)
                        {
                            cell.Value = Convert.ToDouble(value);
                        }
                        else
                        {
                            cell.Value = value.ToString();
                        }

                        if (row % 2 == 1)
                        {
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(242, 242, 242));
                        }
                    }
                }

                if (dt.Columns.Count > 0)
                {
                    ws.View.FreezePanes(2, 1);
                    if (ws.Dimension != null)
                    {
                        ws.Cells[ws.Dimension.Address].AutoFilter = true;
                    }
                    ws.Cells.AutoFitColumns();
                }

                int footerRow = dt.Rows.Count + 3;
                ws.Cells[footerRow, 1].Value = $"Total rows: {dt.Rows.Count}  |  Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                ws.Cells[footerRow, 1].Style.Font.Italic = true;

                package.SaveAs(new FileInfo(filePath));
            }
        }
    }
}
