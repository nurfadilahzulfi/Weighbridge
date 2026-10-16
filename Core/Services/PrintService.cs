using CrystalDecisions.CrystalReports.Engine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public enum TicketPrintType
    {
        Masuk,
        Keluar,
        Reprint
    }

    public interface IPrintService
    {
        bool PrintTicket(string ticketNumber, TicketPrintType printType, out string error);
        bool ExportTicketToPdf(string ticketNumber, TicketPrintType printType, string outputPath, out string error);
    }

    public class PrintService : IPrintService
    {
        private readonly IHanaRepository _hanaRepo;

        private const int CM = 567;
        private const int COL_LEFT = 1250;
        private const int COL_MID = 4750;
        private const int COL_RIGHT = 8800;
        private const int RIGHT_LIMIT = 11040;

        private const int COL_MID_SHIFTED = COL_MID + CM;

        
        private const int COL_RIGHT_SHIFTED = COL_RIGHT + CM;

        private const int COL_CONTRACT = COL_RIGHT_SHIFTED - (CM / 2);

        private const int CONTRACT_WIDTH = 2523;

        private const int CONTRACT_DOWN = CM / 5;

        private const float REPRINT_CONTRACT_FONT = 8.5f;
        private const float LINE_HEIGHT_PER_PT = 23f;

        private const int DRIVER_LEFT = 0;
        private const int DRIVER_WIDTH = 1500;

        private const int VEHICLE_LEFT = 150;
        private const int VEHICLE_WIDTH = 1350;
        private const int COL_2ND_LEFT = 3480 - CM;
        private const int COL_NET_LEFT = 5520 - CM;

        private const int ROW1 = 820, ROW2 = 1220, ROW3 = 1640, ROW4 = 2060;
        private const int ROW_WEIGHT = 2950, ROW_LAB = 3920, ROW_NOTE = 5040;
        private const int ROW_SIGN = 6450, ROW_STAMP = 7100;

        private static readonly bool SHRINK_LONG_TEXT = true;

        public PrintService(IHanaRepository hanaRepo = null)
        {
            _hanaRepo = hanaRepo ?? new HanaRepository();
        }

        public bool PrintTicket(string ticketNumber, TicketPrintType printType, out string error)
        {
            return ProcessTicket(ticketNumber, printType, null, out error);
        }

        public bool ExportTicketToPdf(string ticketNumber, TicketPrintType printType, string outputPath, out string error)
        {
            return ProcessTicket(ticketNumber, printType, outputPath, out error);
        }

        private bool ProcessTicket(string ticketNumber, TicketPrintType printType, string pdfOutputPath, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(ticketNumber))
            {
                error = "Nomor tiket tidak boleh kosong.";
                return false;
            }

            DataTable dt = _hanaRepo.GetPrintTicketData(ticketNumber);
            if (dt == null || dt.Rows.Count == 0)
            {
                error = $"Data tiket '{ticketNumber}' tidak ditemukan di database.";
                return false;
            }

            if (!IsMasukSlip(printType, IsTicketMasukOnly(dt)))
            {
                try { ApplyNoteFallback(dt, _hanaRepo.SearchTicketByTiketNum(ticketNumber)); } catch { }
            }
            var orderedDocNums = new List<string>();
            if (dt.Rows.Count >= 2)
            {
                try
                {
                    DataTable wbRows = _hanaRepo.SearchTicketByTiketNum(ticketNumber);
                    if (wbRows != null && wbRows.Rows.Count >= 2 && wbRows.Columns.Contains("U_SOL_DOCNUM_SAP"))
                    {
                        foreach (DataRow r in SplitTicketRowHelper.GetOrderedRows(wbRows))
                            orderedDocNums.Add(r["U_SOL_DOCNUM_SAP"]?.ToString());
                    }
                }
                catch { }
            }
            BuildContractLines(dt, orderedDocNums, out List<string> contractNos, out List<string> contractQtys);

            DataTable printDt = ConsolidateTicketData(dt);
            bool isMasukOnly = IsTicketMasukOnly(printDt);
            bool splitContracts = ApplySplitContractText(printDt, contractNos, contractQtys, IsMasukSlip(printType, isMasukOnly));
            string contractNoText = splitContracts ? JoinContractValues(contractNos) : "";
            string contractQtyText = splitContracts ? JoinContractValues(contractQtys) : "";
            string reportPath = ResolveReportPath(printType, isMasukOnly, out error);
            if (string.IsNullOrEmpty(reportPath)) return false;

            ReportDocument reportDoc = new ReportDocument();
            try
            {
                reportDoc.Load(reportPath);
                ConfigureReportObjects(reportDoc, printType, isMasukOnly, splitContracts, contractNoText, contractQtyText);

                printDt.TableName = "SOL_SP_ADDON_WB_PRINT_TIKET";
                reportDoc.SetDataSource(printDt);
                reportDoc.SetParameterValue("NOTIKET", ticketNumber);

                if (!string.IsNullOrEmpty(pdfOutputPath))
                {
                    reportDoc.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, pdfOutputPath);
                }
                else
                {
                    if (PrinterSettings.InstalledPrinters.Count == 0)
                    {
                        error = "Tidak ada printer yang terpasang di sistem.";
                        return false;
                    }

                    PrinterSettings defaultSettings = new PrinterSettings();
                    if (string.IsNullOrEmpty(reportDoc.PrintOptions.PrinterName) && defaultSettings.IsValid)
                    {
                        reportDoc.PrintOptions.PrinterName = defaultSettings.PrinterName;
                    }

                    reportDoc.PrintToPrinter(1, false, 0, 0);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                try
                {
                    reportDoc.Close();
                    reportDoc.Dispose();
                }
                catch { }
            }
        }

        private bool IsTicketMasukOnly(DataTable dt)
        {
            if (dt.Rows.Count == 0) return false;
            DataRow row = dt.Rows[0];
            string status = dt.Columns.Contains("Status") ? row["Status"]?.ToString() ?? "" : "";
            string weighout = dt.Columns.Contains("Weighout") ? row["Weighout"]?.ToString() ?? "" : "";
            return string.Equals(status, "WEIGHIN", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(weighout, "N", StringComparison.OrdinalIgnoreCase);
        }

        public static void ApplyNoteFallback(DataTable printDt, DataTable wbRows)
        {
            if (printDt == null || !printDt.Columns.Contains("Note")) return;
            if (wbRows == null || !wbRows.Columns.Contains("U_SOL_NOTE")) return;

            string note = "";
            foreach (DataRow r in wbRows.Rows)
            {
                string v = r["U_SOL_NOTE"] == DBNull.Value ? "" : r["U_SOL_NOTE"]?.ToString()?.Trim() ?? "";
                if (v.Length > 0) { note = v; break; }
            }
            if (note.Length == 0) return;

            DataColumn col = printDt.Columns["Note"];
            col.ReadOnly = false;
            foreach (DataRow row in printDt.Rows)
            {
                if (row[col] == DBNull.Value || string.IsNullOrWhiteSpace(row[col].ToString()))
                    row[col] = note;
            }
        }

        private const string ColSoldTo = "Sold to";
        private const string ColNote = "Note";

        public static void BuildContractLines(DataTable dt, string k1DocNum, out List<string> contractNos, out List<string> contractQtys)
        {
            var order = string.IsNullOrWhiteSpace(k1DocNum) ? new List<string>() : new List<string> { k1DocNum };
            BuildContractLines(dt, order, out contractNos, out contractQtys);
        }

        public static void BuildContractLines(DataTable dt, IList<string> orderedDocNums, out List<string> contractNos, out List<string> contractQtys)
        {
            contractNos = new List<string>();
            contractQtys = new List<string>();
            if (dt == null || !dt.Columns.Contains("Contract No")) return;

            var order = (orderedDocNums ?? new List<string>())
                .Select(d => d?.Trim() ?? "")
                .ToList();
            IEnumerable<DataRow> rows = dt.Rows.Cast<DataRow>().OrderBy(r =>
            {
                string no = Convert.ToString(r["Contract No"], CultureInfo.InvariantCulture)?.Trim() ?? "";
                int idx = order.FindIndex(d => string.Equals(d, no, StringComparison.OrdinalIgnoreCase));
                return idx < 0 ? int.MaxValue : idx;
            });

            foreach (DataRow r in rows)
            {
                string no = r["Contract No"] == DBNull.Value ? "" : Convert.ToString(r["Contract No"], CultureInfo.InvariantCulture).Trim();
                if (no.Length == 0 || no == "0" || contractNos.Contains(no)) continue;
                contractNos.Add(no);

                string qty = "";
                if (dt.Columns.Contains("Contract Qty") && r["Contract Qty"] != DBNull.Value &&
                    decimal.TryParse(Convert.ToString(r["Contract Qty"], CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal q) && q != 0)
                {
                    qty = q.ToString("#,##0.##", CultureInfo.InvariantCulture);
                }
                contractQtys.Add(qty);
            }
        }

        public static string JoinContractValues(IList<string> values)
        {
            return values == null ? "" : string.Join(" / ", values.Where(v => !string.IsNullOrEmpty(v)));
        }

        public static bool ApplySplitContractText(DataTable printDt, IList<string> contractNos, IList<string> contractQtys, bool masukSlip)
        {
            if (printDt == null || printDt.Rows.Count == 0 || contractNos == null || contractNos.Count < 2) return false;
            if (!printDt.Columns.Contains(ColSoldTo) || printDt.Columns[ColSoldTo].DataType != typeof(string)) return false;

            string noText = JoinContractValues(contractNos);
            string qtyText = JoinContractValues(contractQtys);
            DataRow row = printDt.Rows[0];
            DataColumn soldTo = printDt.Columns[ColSoldTo];
            soldTo.ReadOnly = false;
            soldTo.MaxLength = -1;

            if (masukSlip && printDt.Columns.Contains(ColNote) && printDt.Columns[ColNote].DataType == typeof(string))
            {
                DataColumn note = printDt.Columns[ColNote];
                note.ReadOnly = false;
                note.MaxLength = -1;
                row[soldTo] = noText;
                row[note] = qtyText;
            }
            else
            {
                row[soldTo] = noText + "\r\n\r\n" + qtyText;
            }

            foreach (string col in new[] { "Contract No", "Contract Qty" })
            {
                if (!printDt.Columns.Contains(col)) continue;
                printDt.Columns[col].ReadOnly = false;
                row[col] = Convert.ChangeType(0, printDt.Columns[col].DataType, CultureInfo.InvariantCulture);
            }
            return true;
        }

        public static bool IsMasukSlip(TicketPrintType printType, bool isMasukOnly)
        {
            return printType == TicketPrintType.Masuk || (printType == TicketPrintType.Reprint && isMasukOnly);
        }

        private string ResolveReportPath(TicketPrintType printType, bool isMasukOnly, out string error)
        {
            error = "";
            string relativePath;
            if (IsMasukSlip(printType, isMasukOnly))
            {
                relativePath = @"Assets\Report\WeighbridgeTiket-Masuk.rpt";
            }
            else if (printType == TicketPrintType.Keluar)
            {
                relativePath = @"Assets\Report\WeighbridgeTiket-Keluar.rpt";
            }
            else
            {
                relativePath = @"Assets\Report\WeighbridgeTiket.rpt";
            }

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!File.Exists(fullPath)) fullPath = relativePath;

            if (!File.Exists(fullPath))
            {
                error = $"File template laporan tidak ditemukan: {fullPath}";
                return "";
            }
            return fullPath;
        }

        private static bool Is(string name, string target)
        {
            return string.Equals(name, target, StringComparison.OrdinalIgnoreCase);
        }

        private static bool Has(string name, string part)
        {
            return name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void Place(ReportObject obj, int? top = null, int? left = null,
                                  int? width = null, int? minWidth = null, int? maxRight = null)
        {
            try
            {
                if (top.HasValue) obj.Top = top.Value;
                if (left.HasValue) obj.Left = left.Value;

                if (width.HasValue) obj.Width = width.Value;
                else if (minWidth.HasValue && obj.Width < minWidth.Value) obj.Width = minWidth.Value;

                // jangan sampai melewati batas kanan
                if (maxRight.HasValue && obj.Left + obj.Width > maxRight.Value)
                    obj.Width = Math.Max(500, maxRight.Value - obj.Left);
            }
            catch { }
        }

        private static void ShrinkFont(ReportObject obj, float minusPt = 1f)
        {
            if (!SHRINK_LONG_TEXT) return;
            try
            {
                var f = obj as FieldObject;
                if (f == null) return;
                Font cur = f.Font;
                f.ApplyFont(new Font(cur.FontFamily, Math.Max(6f, cur.Size - minusPt), cur.Style));
            }
            catch { }
        }

        private static void SetFontSize(ReportObject obj, float size)
        {
            try
            {
                var f = obj as FieldObject;
                if (f == null) return;
                Font cur = f.Font;
                f.ApplyFont(new Font(cur.FontFamily, size, cur.Style));
            }
            catch { }
        }

        private static float CurrentFontSize(ReportObject obj, float fallback)
        {
            try { return (obj as FieldObject)?.Font?.Size ?? fallback; }
            catch { return fallback; }
        }

        private static float FitFontSize(ReportObject obj, string text, int widthTwips, float maxSize)
        {
            try
            {
                Font cur = (obj as FieldObject)?.Font;
                if (cur == null) return maxSize;
                return FitFontSize(text, cur.FontFamily, cur.Style, widthTwips, maxSize);
            }
            catch { return maxSize; }
        }

        public static float FitFontSize(string text, FontFamily family, FontStyle style, int widthTwips, float maxSize, float minSize = 6f)
        {
            if (string.IsNullOrEmpty(text) || family == null || widthTwips <= 0) return maxSize;

            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            using (var font = new Font(family, 10f, style, GraphicsUnit.Point))
            {
                g.PageUnit = GraphicsUnit.Point;
                float widthPt10 = g.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic).Width;
                if (widthPt10 <= 0) return maxSize;

                float availablePt = widthTwips / 20f * 0.92f;
                float size = (float)Math.Floor(10f * availablePt / widthPt10 * 2f) / 2f;
                return Math.Max(minSize, Math.Min(maxSize, size));
            }
        }

        private static void DumpLayout(ReportDocument doc, string path)
        {
            var sb = new System.Text.StringBuilder();
            foreach (Section s in doc.ReportDefinition.Sections)
                foreach (ReportObject o in s.ReportObjects)
                    sb.AppendLine(string.Format("{0}\t{1}\t{2}\tTop={3}\tLeft={4}\tW={5}\tH={6}",
                        s.Name, o.Name, o.Kind, o.Top, o.Left, o.Width, o.Height));
            File.WriteAllText(path, sb.ToString());
        }

        private void ConfigureReportObjects(ReportDocument reportDoc, TicketPrintType printType, bool isMasukOnly, bool splitContracts = false,
                                            string contractNoText = "", string contractQtyText = "")
        {
            bool suppressKeluarFields = printType == TicketPrintType.Keluar ||
                                        (printType == TicketPrintType.Reprint && !isMasukOnly);
            bool suppressNote = IsMasukSlip(printType, isMasukOnly);
            bool contractInNote = splitContracts && suppressNote;

            try
            {
                foreach (Section s in reportDoc.ReportDefinition.Sections)
                {
                    if (Is(s.Name, "ReportHeaderSection1")) s.Height = 960;
                    else if (Is(s.Name, "PageHeaderSection1") && s.Height < 7500) s.Height = 7500;
                }
            }
            catch { }

            foreach (Section sec in reportDoc.ReportDefinition.Sections)
            {
                foreach (ReportObject obj in sec.ReportObjects)
                {
                    string name = obj.Name ?? "";

                    if (Has(name, "SoldTo"))
                    {
                        try { obj.ObjectFormat.EnableSuppress = !splitContracts; } catch { }
                    }
                    if (splitContracts && (Is(name, "ContractNo1") || Is(name, "ContractQty1")))
                    {
                        try { obj.ObjectFormat.EnableSuppress = true; } catch { }
                    }
                    if (suppressKeluarFields && Has(name, "DoNo"))
                    {
                        try { obj.ObjectFormat.EnableSuppress = true; } catch { }
                    }
                    if (Is(name, "TanggalKeluar1")) Place(obj, ROW1, COL_LEFT);
                    else if (Is(name, "WaktuKeluar1")) Place(obj, ROW2, COL_LEFT);
                    else if (Is(name, "Comodity1"))
                    {
                        Place(obj, ROW3, COL_LEFT, width: COL_MID - COL_LEFT - 200);
                        ShrinkFont(obj);
                    }
                    else if (Is(name, "ReceivedFrom1"))
                    {
                        Place(obj, ROW4, COL_LEFT, width: COL_MID - COL_LEFT - 200);
                        ShrinkFont(obj);
                    }
                    else if (Is(name, "NomorTiket1")) Place(obj, left: COL_LEFT);
                    else if (Is(name, "Soldto1"))
                    {
                        if (!splitContracts) Place(obj, ROW1, COL_MID);
                        else
                        {
                            try { obj.ObjectFormat.EnableCanGrow = true; } catch { }
                            int top = ROW1 + CONTRACT_DOWN;
                            if (contractInNote)
                            {
                                float size = FitFontSize(obj, contractNoText, CONTRACT_WIDTH, CurrentFontSize(obj, 10f));
                                if (size < CurrentFontSize(obj, 10f)) SetFontSize(obj, size);
                            }
                            else
                            {
                                string longest = contractNoText.Length >= contractQtyText.Length ? contractNoText : contractQtyText;
                                float size = FitFontSize(obj, longest, CONTRACT_WIDTH, REPRINT_CONTRACT_FONT);
                                SetFontSize(obj, size);
                                if (size < REPRINT_CONTRACT_FONT)
                                    top += (int)Math.Round((ROW2 - ROW1 - 2 * LINE_HEIGHT_PER_PT * size) / 2);
                            }
                            Place(obj, top, COL_CONTRACT, width: CONTRACT_WIDTH);
                        }
                    }
                    else if (Is(name, "Transporter2")) Place(obj, ROW2, COL_MID_SHIFTED, minWidth: 3000);
                    else if (Is(name, "SealNo1")) Place(obj, ROW3, COL_MID_SHIFTED);
                    else if (Is(name, "DoNo1")) Place(obj, ROW4, COL_MID);
                    else if (Is(name, "ContractNo1")) Place(obj, ROW1 + CONTRACT_DOWN, COL_CONTRACT, width: CONTRACT_WIDTH);
                    else if (Is(name, "ContractQty1")) Place(obj, ROW2 + CONTRACT_DOWN, COL_CONTRACT, width: CONTRACT_WIDTH);
                    else if (Is(name, "TanggalWaktuMasuk1")) Place(obj, ROW3, COL_RIGHT, maxRight: RIGHT_LIMIT);
                    else if (Is(name, "VehicleNo1"))
                    {
                        Place(obj, ROW_WEIGHT, VEHICLE_LEFT, width: VEHICLE_WIDTH);
                        try
                        {
                            obj.ObjectFormat.HorizontalAlignment = CrystalDecisions.Shared.Alignment.LeftAlign;
                        }
                        catch { }
                    }
                    else if (Is(name, "FirstWeight1")) Place(obj, ROW_WEIGHT, 1560, width: 1560);
                    else if (Is(name, "SecondWeight1")) Place(obj, ROW_WEIGHT, COL_2ND_LEFT, width: 1800);
                    else if (Is(name, "NetWeight1")) Place(obj, ROW_WEIGHT, COL_NET_LEFT, width: 1680);
                    else if (Is(name, "MillsNetWeight1")) Place(obj, ROW_WEIGHT, 7440, width: 1680);
                    else if (Is(name, "Difference1")) Place(obj, ROW_WEIGHT, 9200, width: 1680);
                    else if (Is(name, "LabFFA1") || Is(name, "LabMI1") || Is(name, "LabIV1"))
                        Place(obj, ROW_LAB);
                    else if (Is(name, "LabCP1")) Place(obj, ROW_LAB, 9360);
                    else if (Has(name, "Note"))
                    {
                        if (contractInNote)
                        {
                            try
                            {
                                obj.ObjectFormat.EnableSuppress = false;
                                obj.ObjectFormat.EnableCanGrow = false;
                            }
                            catch { }
                            Place(obj, ROW2 + CONTRACT_DOWN, COL_CONTRACT, width: CONTRACT_WIDTH);
                            try { obj.Height = 240; } catch { }
                            float size = FitFontSize(obj, contractQtyText, CONTRACT_WIDTH, CurrentFontSize(obj, 10f));
                            if (size < CurrentFontSize(obj, 10f)) SetFontSize(obj, size);
                        }
                        else
                        {
                            try
                            {
                                obj.ObjectFormat.EnableSuppress = suppressNote;
                                obj.ObjectFormat.EnableCanGrow = true;
                            }
                            catch { }
                            Place(obj, ROW_NOTE, 240, minWidth: 10920);
                        }
                    }
                    else if (Is(name, "User2")) Place(obj, ROW_SIGN, 3360, minWidth: 1600);
                    else if (Is(name, "Driver1") ||
                             (Has(name, "Driver") && obj.Kind == CrystalDecisions.Shared.ReportObjectKind.FieldObject))
                    {
                        Place(obj, ROW_SIGN, DRIVER_LEFT, width: DRIVER_WIDTH);
                        try
                        {
                            obj.ObjectFormat.HorizontalAlignment = CrystalDecisions.Shared.Alignment.LeftAlign;
                        }
                        catch { }
                    }
                    else if (Is(name, "Text1")) Place(obj, ROW_STAMP, 3240);
                }
            }

            try
            {
                string flag = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dump-layout.flag");
                if (File.Exists(flag))
                    DumpLayout(reportDoc, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout-dump.txt"));
            }
            catch { }
        }

        public static DataTable ConsolidateTicketData(DataTable dt)
        {
            if (dt == null || dt.Rows.Count <= 1)
            {
                return dt;
            }

            DataTable consolidatedDt = dt.Clone();
            DataRow baseRow = consolidatedDt.NewRow();
            baseRow.ItemArray = (object[])dt.Rows[0].ItemArray.Clone();
            foreach (DataColumn col in dt.Columns)
            {
                if (!IsBlankCell(baseRow[col.ColumnName], col.ColumnName)) continue;
                for (int i = 1; i < dt.Rows.Count; i++)
                {
                    if (!IsBlankCell(dt.Rows[i][col], col.ColumnName))
                    {
                        baseRow[col.ColumnName] = dt.Rows[i][col];
                        break;
                    }
                }
            }

            double totalNetWeight = 0;
            double totalMillsNetWeight = 0;
            decimal totalContractQty = 0;
            double maxFirstWeight = 0;
            double maxSecondWeight = 0;

            var contractNumbers = new System.Collections.Generic.List<string>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];

                if (dt.Columns.Contains("Net Weight"))
                {
                    totalNetWeight += ExtractWeightKg(row["Net Weight"]);
                }

                if (dt.Columns.Contains("Mills Net Weight"))
                {
                    totalMillsNetWeight += ExtractWeightKg(row["Mills Net Weight"]);
                }

                if (dt.Columns.Contains("Contract Qty") && row["Contract Qty"] != DBNull.Value)
                {
                    totalContractQty += Convert.ToDecimal(row["Contract Qty"]);
                }

                if (dt.Columns.Contains("First Weight"))
                {
                    double fw = ExtractWeightKg(row["First Weight"]);
                    if (fw > maxFirstWeight) maxFirstWeight = fw;
                }

                if (dt.Columns.Contains("Second Weight"))
                {
                    double sw = ExtractWeightKg(row["Second Weight"]);
                    if (sw > maxSecondWeight) maxSecondWeight = sw;
                }

                if (dt.Columns.Contains("Contract No") && row["Contract No"] != DBNull.Value)
                {
                    string cNo = row["Contract No"].ToString().Trim();
                    if (!IsBlankCell(cNo, "Contract No") && !contractNumbers.Contains(cNo))
                    {
                        contractNumbers.Add(cNo);
                    }
                }
            }

            if (dt.Columns.Contains("First Weight") && maxFirstWeight > 0)
            {
                baseRow["First Weight"] = FormatWeightKg(maxFirstWeight);
            }

            if (dt.Columns.Contains("Second Weight") && maxSecondWeight > 0)
            {
                baseRow["Second Weight"] = FormatWeightKg(maxSecondWeight);
            }

            if (totalNetWeight == 0 && maxFirstWeight > 0 && maxSecondWeight > 0)
            {
                totalNetWeight = Math.Abs(maxFirstWeight - maxSecondWeight);
            }

            if (dt.Columns.Contains("Net Weight") && totalNetWeight > 0)
            {
                baseRow["Net Weight"] = FormatWeightKg(totalNetWeight);
            }

            if (dt.Columns.Contains("Mills Net Weight") && totalMillsNetWeight > 0)
            {
                baseRow["Mills Net Weight"] = FormatWeightKg(totalMillsNetWeight);
            }

            if (dt.Columns.Contains("Difference"))
            {
                double diff = totalNetWeight - totalMillsNetWeight;
                baseRow["Difference"] = FormatWeightKg(diff);
            }

            if (dt.Columns.Contains("Contract Qty") && totalContractQty > 0)
            {
                baseRow["Contract Qty"] = totalContractQty;
            }

            if (dt.Columns.Contains("Note") && dt.Rows.Count > 1)
            {
                var splitItems = new System.Collections.Generic.List<string>();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    string vendorRef = "";
                    if (dt.Columns.Contains("Vendor Ref") && row["Vendor Ref"] != DBNull.Value)
                    {
                        vendorRef = row["Vendor Ref"].ToString().Trim();
                    }
                    if (string.IsNullOrEmpty(vendorRef) && dt.Columns.Contains("VendorRef") && row["VendorRef"] != DBNull.Value)
                    {
                        vendorRef = row["VendorRef"].ToString().Trim();
                    }
                    if (string.IsNullOrEmpty(vendorRef) && dt.Columns.Contains("U_SOL_VENDOR_REF") && row["U_SOL_VENDOR_REF"] != DBNull.Value)
                    {
                        vendorRef = row["U_SOL_VENDOR_REF"].ToString().Trim();
                    }

                    string cNo = "";
                    if (dt.Columns.Contains("Contract No") && row["Contract No"] != DBNull.Value)
                    {
                        cNo = row["Contract No"].ToString().Trim();
                    }
                    if (string.IsNullOrEmpty(cNo) && dt.Columns.Contains("Do No") && row["Do No"] != DBNull.Value)
                    {
                        cNo = row["Do No"].ToString().Trim();
                    }

                    string itemLabel = !string.IsNullOrEmpty(vendorRef) ? vendorRef : cNo;

                    double netKg = 0;
                    if (dt.Columns.Contains("Net Weight"))
                    {
                        netKg = ExtractWeightKg(row["Net Weight"]);
                    }
                    if (netKg <= 0 && dt.Columns.Contains("Mills Net Weight"))
                    {
                        netKg = ExtractWeightKg(row["Mills Net Weight"]);
                    }

                    if (!string.IsNullOrEmpty(itemLabel))
                    {
                        if (netKg > 0)
                        {
                            splitItems.Add($"{itemLabel}: {FormatNumberWithThousands(netKg)} KG");
                        }
                        else
                        {
                            splitItems.Add(itemLabel);
                        }
                    }
                }

                if (splitItems.Count > 1)
                {
                    string newSplitNote = string.Join(" SPLIT ", splitItems);
                    string existingNote = baseRow["Note"]?.ToString() ?? "";

                    string oldSplitPattern = string.Join(" SPLIT ", contractNumbers);
                    if (string.IsNullOrWhiteSpace(existingNote) ||
                        string.Equals(existingNote.Trim(), oldSplitPattern.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(existingNote.Trim(), $"({oldSplitPattern})".Trim(), StringComparison.OrdinalIgnoreCase) ||
                        System.Text.RegularExpressions.Regex.IsMatch(existingNote.Trim(), @"^\(?.*SPLIT.*KG\)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                        System.Text.RegularExpressions.Regex.IsMatch(existingNote.Trim(), @"^\(?\d+\s+SPLIT\s+\d+\)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        baseRow["Note"] = newSplitNote;
                    }
                    else if (!string.IsNullOrEmpty(oldSplitPattern) && existingNote.Contains(oldSplitPattern))
                    {
                        baseRow["Note"] = existingNote.Replace(oldSplitPattern, newSplitNote);
                    }
                    else if (!existingNote.Contains(newSplitNote))
                    {
                        baseRow["Note"] = $"{existingNote} ({newSplitNote})";
                    }
                }
            }

            consolidatedDt.Rows.Add(baseRow);
            consolidatedDt.TableName = "SOL_SP_ADDON_WB_PRINT_TIKET";
            return consolidatedDt;
        }

        private static bool IsBlankCell(object value, string columnName)
        {
            if (value == null || value == DBNull.Value) return true;
            string s = value.ToString().Trim();
            if (s == "") return true;
            if ((columnName == "Contract No" || columnName == "Contract Qty") && (s == "0" || s == "0.000000")) return true;
            return false;
        }

        public static double ExtractWeightKg(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            string s = value.ToString().Trim();
            if (s.EndsWith("kg", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 2).Trim();
            }
            return Weighbridge.Core.Helpers.DataConversionHelper.SafeToDouble(s);
        }

        public static string FormatWeightKg(double kg)
        {
            if (Math.Abs(kg - Math.Round(kg)) < 0.0001)
            {
                return $"{(long)Math.Round(kg)} kg";
            }
            return $"{kg.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} kg";
        }

        public static string FormatNumberWithThousands(double qty)
        {
            if (Math.Abs(qty - Math.Round(qty)) < 0.0001)
            {
                return ((long)Math.Round(qty)).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            }
            return qty.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}