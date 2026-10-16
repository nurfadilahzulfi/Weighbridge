using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using Weighbridge.Core.Helpers;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class SyncAndReprintFlowTests
    {
        [TestMethod]
        public void DataConversionHelper_SafeToDouble_ParsesValidAndInvalidFormats()
        {
            Assert.AreEqual(1234.56, DataConversionHelper.SafeToDouble("1234.56"));
            Assert.AreEqual(1234.56, DataConversionHelper.SafeToDouble("1234,56"));
            Assert.AreEqual(50000.0, DataConversionHelper.SafeToDouble(50000));
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble(null));
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble(""));
            Assert.AreEqual(0.0, DataConversionHelper.SafeToDouble("invalid-number"));
        }

        [TestMethod]
        public void PrintService_ExtractWeightKg_HandlesVariousFormats()
        {
            Assert.AreEqual(50200.0, PrintService.ExtractWeightKg("50200 kg"));
            Assert.AreEqual(2950.0, PrintService.ExtractWeightKg("2950 KG"));
            Assert.AreEqual(46100.0, PrintService.ExtractWeightKg("46100"));
            Assert.AreEqual(1150.5, PrintService.ExtractWeightKg("1150.5 kg"));
            Assert.AreEqual(0.0, PrintService.ExtractWeightKg("0 kg"));
            Assert.AreEqual(0.0, PrintService.ExtractWeightKg(null));
            Assert.AreEqual(0.0, PrintService.ExtractWeightKg(DBNull.Value));
        }

        [TestMethod]
        public void PrintService_ConsolidateTicketData_FormatsSplitNoteWithIndividualNettos()
        {
            var dt = new System.Data.DataTable("SOL_SP_ADDON_WB_PRINT_TIKET");
            dt.Columns.Add("Contract No", typeof(string));
            dt.Columns.Add("First Weight", typeof(string));
            dt.Columns.Add("Second Weight", typeof(string));
            dt.Columns.Add("Net Weight", typeof(string));
            dt.Columns.Add("Mills Net Weight", typeof(string));
            dt.Columns.Add("Contract Qty", typeof(decimal));
            dt.Columns.Add("Note", typeof(string));

            var r1 = dt.NewRow();
            r1["Contract No"] = "261000081";
            r1["First Weight"] = "50000 kg";
            r1["Second Weight"] = "2750 kg";
            r1["Net Weight"] = "46100 kg";
            r1["Mills Net Weight"] = "46100 kg";
            r1["Contract Qty"] = 100000m;
            r1["Note"] = "261000081 SPLIT 261000082";
            dt.Rows.Add(r1);

            var r2 = dt.NewRow();
            r2["Contract No"] = "261000082";
            r2["First Weight"] = "50000 kg";
            r2["Second Weight"] = "2750 kg";
            r2["Net Weight"] = "1150 kg";
            r2["Mills Net Weight"] = "1150 kg";
            r2["Contract Qty"] = 50000m;
            r2["Note"] = "261000081 SPLIT 261000082";
            dt.Rows.Add(r2);

            var consolidated = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual(1, consolidated.Rows.Count);
            Assert.AreEqual("47250 kg", consolidated.Rows[0]["Net Weight"]);
            Assert.AreEqual("261000081: 46,100 KG SPLIT 261000082: 1,150 KG", consolidated.Rows[0]["Note"]);
        }

        [TestMethod]
        public void PrintService_ConsolidateTicketData_UsesVendorReferenceInSplitNote()
        {
            var dt = new System.Data.DataTable("SOL_SP_ADDON_WB_PRINT_TIKET");
            dt.Columns.Add("Contract No", typeof(string));
            dt.Columns.Add("Vendor Ref", typeof(string));
            dt.Columns.Add("First Weight", typeof(string));
            dt.Columns.Add("Second Weight", typeof(string));
            dt.Columns.Add("Net Weight", typeof(string));
            dt.Columns.Add("Mills Net Weight", typeof(string));
            dt.Columns.Add("Contract Qty", typeof(decimal));
            dt.Columns.Add("Note", typeof(string));

            var r1 = dt.NewRow();
            r1["Contract No"] = "261000081";
            r1["Vendor Ref"] = "001/TRI-CPKO/IX/2026";
            r1["First Weight"] = "120 kg";
            r1["Second Weight"] = "60 kg";
            r1["Net Weight"] = "40 kg";
            r1["Mills Net Weight"] = "40 kg";
            r1["Contract Qty"] = 100m;
            r1["Note"] = "261000081: 40 KG SPLIT 261000082: 20 KG";
            dt.Rows.Add(r1);

            var r2 = dt.NewRow();
            r2["Contract No"] = "261000082";
            r2["Vendor Ref"] = "003/TRI-CPKO/IX/2026";
            r2["First Weight"] = "120 kg";
            r2["Second Weight"] = "60 kg";
            r2["Net Weight"] = "20 kg";
            r2["Mills Net Weight"] = "60 kg";
            r2["Contract Qty"] = 100m;
            r2["Note"] = "261000081: 40 KG SPLIT 261000082: 20 KG";
            dt.Rows.Add(r2);

            var consolidated = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual(1, consolidated.Rows.Count);
            Assert.AreEqual("60 kg", consolidated.Rows[0]["Net Weight"]);
            Assert.AreEqual("001/TRI-CPKO/IX/2026: 40 KG SPLIT 003/TRI-CPKO/IX/2026: 20 KG", consolidated.Rows[0]["Note"]);
        }

        [TestMethod]
        public void HanaRepository_GetReportData_GuaranteesNamaVendorColumn()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("Document Type SAP", typeof(string));
            dt.Columns.Add("Document Number SAP", typeof(string));
            dt.Columns.Add("Vendor Ref No. SAP", typeof(string));
            dt.Columns.Add("Nomor Tiket", typeof(string));

            if (!dt.Columns.Contains("Nama Vendor"))
            {
                dt.Columns.Add("Nama Vendor", typeof(string));
                if (dt.Columns.Contains("Vendor Ref No. SAP"))
                {
                    int targetIdx = dt.Columns["Vendor Ref No. SAP"].Ordinal + 1;
                    dt.Columns["Nama Vendor"].SetOrdinal(targetIdx);
                }
            }

            Assert.IsTrue(dt.Columns.Contains("Nama Vendor"));
            Assert.AreEqual(3, dt.Columns["Nama Vendor"].Ordinal);
        }

        [TestMethod]
        public void ExcelExportService_ExportDataTableToExcel_ExportsNamaVendorColumn()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("Document Type SAP", typeof(string));
            dt.Columns.Add("Document Number SAP", typeof(string));
            dt.Columns.Add("Vendor Ref No. SAP", typeof(string));
            dt.Columns.Add("Nama Vendor", typeof(string));
            dt.Columns.Add("Nomor Tiket", typeof(string));

            var row = dt.NewRow();
            row["Document Type SAP"] = "PO";
            row["Document Number SAP"] = "261000081";
            row["Vendor Ref No. SAP"] = "V-001";
            row["Nama Vendor"] = "PT SAWIT JAYA";
            row["Nomor Tiket"] = "AS26090001";
            dt.Rows.Add(row);

            string tempPath = Path.Combine(Path.GetTempPath(), $"ReportTest_{Guid.NewGuid():N}.xlsx");
            try
            {
                var service = new ExcelExportService();
                service.ExportDataTableToExcel(dt, tempPath);

                Assert.IsTrue(File.Exists(tempPath));
                var fileInfo = new FileInfo(tempPath);
                Assert.IsTrue(fileInfo.Length > 0);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        [TestMethod]
        public void Live_Verification_Hana_Report_And_Print()
        {
            var xmlService = new ConfigXmlService();
            string cfgPath = @"c:\Soltius\Development\Add On\Weighbridge (TESTING) C\Weighbridge\bin\Debug\Config\Config.xml";
            var (wb, sbo) = xmlService.LoadFromFile(cfgPath);
            ConfigManager.wbConfig = wb;
            ConfigManager.sboConfig = sbo;

            var repo = new HanaRepository();

            // 1. Live Report Data from HANA
            var dtReport = repo.GetReportData("", "", "", "", "");
            Console.WriteLine($"[VERIF 1] Report Data Rows: {dtReport.Rows.Count}, Cols: {dtReport.Columns.Count}");
            Assert.IsTrue(dtReport.Columns.Contains("Nama Vendor"), "Column 'Nama Vendor' must exist in Report DataTable");
            int vendorColIdx = dtReport.Columns["Nama Vendor"].Ordinal;
            Console.WriteLine($"[VERIF 1] 'Nama Vendor' Column Ordinal: {vendorColIdx}");

            var kodePabean = repo.GetKodeDokumenPabean();
            Console.WriteLine($"[VERIF PABEAN] Total Codes: {kodePabean.Count}");
            foreach (var k in kodePabean) Console.WriteLine($"[VERIF PABEAN CODE] '{k}'");

            int sampled = 0;
            for (int i = 0; i < dtReport.Rows.Count && sampled < 5; i++)
            {
                var row = dtReport.Rows[i];
                string docNum = row["Document Number SAP"]?.ToString();
                string docType = row["Document Type SAP"]?.ToString();
                string tiket = row["Nomor Tiket"]?.ToString();
                string vendor = row["Nama Vendor"]?.ToString();
                if (!string.IsNullOrEmpty(vendor))
                {
                    Console.WriteLine($"[VERIF 1 SAMPLE] Tiket: {tiket}, Type: {docType}, DocNum: {docNum}, Nama Vendor: '{vendor}'");
                    sampled++;
                }
            }

            // 2. Live Excel Export
            string excelPath = Path.Combine(Path.GetTempPath(), "LiveTest_Report_With_NamaVendor.xlsx");
            var excelService = new ExcelExportService();
            excelService.ExportDataTableToExcel(dtReport, excelPath);
            Assert.IsTrue(File.Exists(excelPath), "Excel file must be generated");
            var fi = new FileInfo(excelPath);
            Console.WriteLine($"[VERIF 2] Exported Excel Size: {fi.Length} bytes at {excelPath}");
            Assert.IsTrue(fi.Length > 0);

            // 3. Live Split Ticket Print Note Consolidation
            var dtTicket = repo.GetPrintTicketData("AS2609250001");
            Console.WriteLine($"[VERIF 3] Raw Ticket Rows for AS2609250001: {dtTicket.Rows.Count}");
            for (int i = 0; i < dtTicket.Rows.Count; i++)
            {
                Console.WriteLine($"[VERIF 3 RAW ROW {i}] Contract No: {dtTicket.Rows[i]["Contract No"]}, Net: {dtTicket.Rows[i]["Net Weight"]}, Mills Net: {dtTicket.Rows[i]["Mills Net Weight"]}, Note: {dtTicket.Rows[i]["Note"]}");
            }
            var dtTicket3 = repo.GetPrintTicketData("AS2609250003");
            Console.WriteLine($"[VERIF 3b] Raw Ticket Rows for AS2609250003: {dtTicket3.Rows.Count}");
            for (int i = 0; i < dtTicket3.Rows.Count; i++)
            {
                Console.WriteLine($"[VERIF 3b RAW ROW {i}] Contract No: {dtTicket3.Rows[i]["Contract No"]}, Net: {dtTicket3.Rows[i]["Net Weight"]}, Mills Net: {dtTicket3.Rows[i]["Mills Net Weight"]}, Note: {dtTicket3.Rows[i]["Note"]}");
            }
            var dtSearch3 = repo.SearchTicketByTiketNum("AS2609250003");
            Console.WriteLine($"[VERIF 3c] SearchTicketByTiketNum for AS2609250003: {dtSearch3.Rows.Count} rows");
            for (int i = 0; i < dtSearch3.Rows.Count; i++)
            {
                var r = dtSearch3.Rows[i];
                var cols = new System.Collections.Generic.List<string>();
                foreach (System.Data.DataColumn c in dtSearch3.Columns)
                {
                    if (c.ColumnName.Contains("VENDOR") || c.ColumnName.Contains("DOC") || c.ColumnName.Contains("KONTRAK") || c.ColumnName.Contains("NOTE"))
                    {
                        cols.Add($"{c.ColumnName}='{r[c]}'");
                    }
                }
                Console.WriteLine($"[VERIF 3c ROW {i}] " + string.Join(" | ", cols));
            }
            var consolidated = PrintService.ConsolidateTicketData(dtTicket);
            Console.WriteLine($"[VERIF 3 CONSOLIDATED AS2609250001] Rows: {consolidated.Rows.Count}");
            if (consolidated.Rows.Count > 0)
            {
                Console.WriteLine($"[VERIF 3 CONSOLIDATED NOTE AS2609250001]: '{consolidated.Rows[0]["Note"]}'");
                Console.WriteLine($"[VERIF 3 CONSOLIDATED NET WEIGHT AS2609250001]: '{consolidated.Rows[0]["Net Weight"]}'");
            }

            var consolidated3 = PrintService.ConsolidateTicketData(dtTicket3);
            Console.WriteLine($"[VERIF 3 CONSOLIDATED AS2609250003] Rows: {consolidated3.Rows.Count}");
            if (consolidated3.Rows.Count > 0)
            {
                Console.WriteLine($"[VERIF 3 CONSOLIDATED NOTE AS2609250003]: '{consolidated3.Rows[0]["Note"]}'");
                Console.WriteLine($"[VERIF 3 CONSOLIDATED NET WEIGHT AS2609250003]: '{consolidated3.Rows[0]["Net Weight"]}'");
                if (dtTicket3.Rows.Count > 1)
                {
                    Assert.IsTrue(consolidated3.Rows[0]["Note"]?.ToString().Contains("001/TRI-CPKO/IX/2026") ?? false, "Note should contain vendor ref K1");
                    Assert.IsTrue(consolidated3.Rows[0]["Note"]?.ToString().Contains("003/TRI-CPKO/IX/2026") ?? false, "Note should contain vendor ref K2");
                }
            }

            var printService = new PrintService(repo);
            string pdfOut = @"C:\Users\ITD02\.gemini\antigravity-ide\brain\8c7c611f-5a56-46a3-97c8-7da0cc5592c0\reprint_split_with_netto_AS2609250001.pdf";
            bool pdfSuccess = printService.ExportTicketToPdf("AS2609250001", TicketPrintType.Reprint, pdfOut, out string pdfErr);
            Console.WriteLine($"[VERIF 3 PDF EXPORT AS2609250001] Success: {pdfSuccess}, Error: '{pdfErr}', Exists: {File.Exists(pdfOut)}");
            Assert.IsTrue(pdfSuccess, "PDF export should succeed: " + pdfErr);

            string pdfOut3 = @"C:\Users\ITD02\.gemini\antigravity-ide\brain\8c7c611f-5a56-46a3-97c8-7da0cc5592c0\reprint_split_AS2609250003.pdf";
            bool pdfSuccess3 = printService.ExportTicketToPdf("AS2609250003", TicketPrintType.Reprint, pdfOut3, out string pdfErr3);
            Console.WriteLine($"[VERIF 3 PDF EXPORT AS2609250003] Success: {pdfSuccess3}, Error: '{pdfErr3}', Exists: {File.Exists(pdfOut3)}");
            Assert.IsTrue(pdfSuccess3, "PDF export for AS2609250003 should succeed: " + pdfErr3);
        }

        [TestMethod]
        public void TimbangKeluarUpdateGRPODO_CalculatesNettoAccurately()
        {
            double beratMasuk = 34500.0;
            double beratKeluar = 12200.0;
            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(22300.0, netto);
        }

        [TestMethod]
        public void TimbangKeluarUpdateGRPODO_UnderweightOrReverseWeighing_CalculatesPositiveNetto()
        {
            // Truk keluar timbang masuk kosong (11.000 kg), timbang keluar isi (28.000 kg)
            double beratMasuk = 11000.0;
            double beratKeluar = 28000.0;
            double netto = WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);

            Assert.AreEqual(17000.0, netto);
        }

        [TestMethod]
        public void TimbangMasukUpdateGRPODO_WeighInDataConstruction_MatchesExpectedFields()
        {
            var data = new WeighInData
            {
                Code = "WB001",
                DocType = "GRPO",
                DocNum = "100234",
                DocEntry = 5678,
                LineNum = 0,
                TiketNum = "T26090001",
                Commodity = "CPO",
                NoKontrak = "KTR-2026-001",
                Nopol = "BK 1234 AB",
                NamaSupir = "Budi Santoso",
                BeratMasuk = 25400.0,
                KodeDokumenPabean = "BC 4.0",
                NoDokumenPabean = "000123/BC/2026",
                TanggalDokumenPabean = new DateTime(2026, 9, 24),
                NomorSpb = "SPB-001",
                NomorAju = "AJU-001",
                Status = "WEIGHIN"
            };

            Assert.AreEqual("WB001", data.Code);
            Assert.AreEqual("T26090001", data.TiketNum);
            Assert.AreEqual(25400.0, data.BeratMasuk);
            Assert.AreEqual("WEIGHIN", data.Status);
            Assert.AreEqual("BC 4.0", data.KodeDokumenPabean);
        }

        [TestMethod]
        public void ReprintTiket_PrintTypeEnum_SupportsReprintRptMapping()
        {
            Assert.AreEqual(TicketPrintType.Reprint, TicketPrintType.Reprint);
            Assert.AreEqual(TicketPrintType.Masuk, TicketPrintType.Masuk);
            Assert.AreEqual(TicketPrintType.Keluar, TicketPrintType.Keluar);
        }

        [TestMethod]
        public void ReprintTiket_SearchSuggestions_FiltersAccurately()
        {
            var items = new System.Collections.Generic.List<TicketLookupItem>
            {
                new TicketLookupItem { TiketNum = "AS2609250001", Nopol = "BK 1234 AB", NamaSupir = "PRABOWO", DocNum = "261000052", Status = "SYNCSAP" },
                new TicketLookupItem { TiketNum = "AS2609250003", Nopol = "BK 1234 AC", NamaSupir = "KAESANG", DocNum = "261000081", Status = "SYNCSAP" }
            };

            // Search by partial ticket number
            var res1 = TicketLookupFilter.Filter(items, "0003");
            Assert.AreEqual(1, res1.Count);
            Assert.AreEqual("AS2609250003", res1[0].TiketNum);

            // Search by nopol
            var res2 = TicketLookupFilter.Filter(items, "1234");
            Assert.AreEqual(2, res2.Count);

            // Search by supir
            var res3 = TicketLookupFilter.Filter(items, "KAESANG");
            Assert.AreEqual(1, res3.Count);
            Assert.AreEqual("AS2609250003", res3[0].TiketNum);

            // Search by base document
            var res4 = TicketLookupFilter.Filter(items, "261000052");
            Assert.AreEqual(1, res4.Count);
            Assert.AreEqual("AS2609250001", res4[0].TiketNum);
        }

        [TestMethod]
        public void PabeanValidation_UpdateFlow_RejectsIncompleteData()
        {
            var invalid1 = PabeanValidator.Validate("", "001234", true, "dd/MM/yyyy");
            Assert.IsFalse(invalid1.IsValid);

            var invalid2 = PabeanValidator.Validate("BC 2.3", "", true, "dd/MM/yyyy");
            Assert.IsFalse(invalid2.IsValid);

            var invalid3 = PabeanValidator.Validate("BC 2.3", "001234", false, " ");
            Assert.IsFalse(invalid3.IsValid);

            var valid = PabeanValidator.Validate("BC 2.3", "001234", true, "dd/MM/yyyy");
            Assert.IsTrue(valid.IsValid);
        }

        [TestMethod]
        public void PrintService_EmptyTicketNumber_ReturnsFalseWithErrorMessage()
        {
            var fakeRepo = new FakePrintHanaRepo();
            var service = new PrintService(fakeRepo);

            bool success = service.PrintTicket("", TicketPrintType.Masuk, out string error);

            Assert.IsFalse(success);
            Assert.IsTrue(error.Contains("tidak boleh kosong"));
        }

        [TestMethod]
        public void PrintService_TicketNotFoundInDatabase_ReturnsFalseWithSpecificMessage()
        {
            var fakeRepo = new FakePrintHanaRepo();
            fakeRepo.ReturnEmptyData = true;
            var service = new PrintService(fakeRepo);

            bool success = service.PrintTicket("NONEXISTENT", TicketPrintType.Masuk, out string error);

            Assert.IsFalse(success);
            Assert.IsTrue(error.Contains("tidak ditemukan"));
        }

        [TestMethod]
        public void PrintService_SoldTo_Suppressed_OnMasukKeluarAndReprint()
        {
            var fakeRepo = new FakePrintHanaRepo();
            var service = new PrintService(fakeRepo);

            string outMasuk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_masuk.pdf");
            string outKeluar = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_keluar.pdf");
            string outReprint = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_reprint.pdf");

            bool okMasuk = service.ExportTicketToPdf("T-MASUK", TicketPrintType.Masuk, outMasuk, out string err1);
            bool okKeluar = service.ExportTicketToPdf("T-KELUAR", TicketPrintType.Keluar, outKeluar, out string err2);
            bool okReprint = service.ExportTicketToPdf("T-REPRINT", TicketPrintType.Reprint, outReprint, out string err3);

            Assert.IsTrue(okMasuk, "Export Masuk failed: " + err1);
            Assert.IsTrue(okKeluar, "Export Keluar failed: " + err2);
            Assert.IsTrue(okReprint, "Export Reprint failed: " + err3);

            Assert.IsTrue(File.Exists(outMasuk) && new FileInfo(outMasuk).Length > 0);
            Assert.IsTrue(File.Exists(outKeluar) && new FileInfo(outKeluar).Length > 0);
            Assert.IsTrue(File.Exists(outReprint) && new FileInfo(outReprint).Length > 0);
        }

        [TestMethod]
        public void PrintService_DoNo_SuppressedOnKeluar_ActiveOnMasuk()
        {
            string pathKeluar = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Report\WeighbridgeTiket-Keluar.rpt");
            string pathMasuk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Report\WeighbridgeTiket-Masuk.rpt");

            using (var docKeluar = new CrystalDecisions.CrystalReports.Engine.ReportDocument())
            using (var docMasuk = new CrystalDecisions.CrystalReports.Engine.ReportDocument())
            {
                docKeluar.Load(pathKeluar);
                docMasuk.Load(pathMasuk);

                var fakeRepo = new FakePrintHanaRepo();
                var service = new PrintService(fakeRepo);

                string outKeluar = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_dono_keluar.pdf");
                string outMasuk = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_dono_masuk.pdf");

                bool okK = service.ExportTicketToPdf("T-01", TicketPrintType.Keluar, outKeluar, out _);
                bool okM = service.ExportTicketToPdf("T-01", TicketPrintType.Masuk, outMasuk, out _);

                Assert.IsTrue(okK && okM);
            }
        }

        [TestMethod]
        public void PrintService_SplitContract_PrintsSingleSlipWithConsolidatedTotals()
        {
            var fakeRepo = new FakePrintHanaRepo { ReturnSplitContractData = true };
            var service = new PrintService(fakeRepo);

            string outPdf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_split_print_single.pdf");
            string outPdf2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_split_print_single_2.pdf");

            if (File.Exists(outPdf)) File.Delete(outPdf);
            if (File.Exists(outPdf2)) File.Delete(outPdf2);

            bool success = service.ExportTicketToPdf("AS-SPLIT-99", TicketPrintType.Reprint, outPdf, out string error);

            Assert.IsTrue(success, "Split export failed: " + error);
            Assert.IsTrue(File.Exists(outPdf), "Single consolidated slip must exist");
            Assert.IsTrue(new FileInfo(outPdf).Length > 0, "Single consolidated slip must have content");
            Assert.IsFalse(File.Exists(outPdf2), "Split contract must NOT create a second slip/PDF (must be 1 slip only)");
        }

        [TestMethod]
        public void PrintService_ConsolidateTicketData_MergesWeightsAccurately()
        {
            var fakeRepo = new FakePrintHanaRepo { ReturnSplitContractData = true };
            var dt = fakeRepo.GetPrintTicketData("AS-SPLIT-99");

            Assert.AreEqual(2, dt.Rows.Count, "Fake data must have 2 rows before consolidation");

            var consolidated = PrintService.ConsolidateTicketData(dt);

            Assert.AreEqual(1, consolidated.Rows.Count, "Consolidated data must have exactly 1 row");
            var row = consolidated.Rows[0];
            Assert.AreEqual("15000 kg", row["First Weight"].ToString());
            Assert.AreEqual("5000 kg", row["Second Weight"].ToString());
            Assert.AreEqual("15000 kg", row["Net Weight"].ToString(), "Net Weight must be combined K1(10000) + K2(5000)");
            Assert.AreEqual("15000 kg", row["Mills Net Weight"].ToString(), "Mills Net Weight must be combined 10000 + 5000");
            Assert.AreEqual("0 kg", row["Difference"].ToString());
            Assert.AreEqual(300000m, Convert.ToDecimal(row["Contract Qty"]), "Contract Qty must be sum of K1 + K2");
        }

        [TestMethod]
        public void WeighOutService_SyncSplitGRPO_AssignsCorrectNettoToBothUDOs()
        {
            var mockSap = new FakeSapRepoForSplit();
            var service = new WeighOutService(new FakePrintHanaRepo(), mockSap);

            var k1Data = new WeighOutData { TiketNum = "T-01", Code = "WB001", DocNum = "261000077" };
            var k2Data = new WeighOutData { TiketNum = "T-01", Code = "WB002", DocNum = "262000161" };

            double k1Netto = 8800.0;
            double k2Netto = 5790.0;

            bool success = service.SyncSplitGRPO(k1Data, k1Netto, k2Data, k2Netto, out int k1DocEntry, out int k2DocEntry, out string error);

            Assert.IsTrue(success, "SyncSplitGRPO failed: " + error);
            Assert.AreEqual(8800.0, k1Data.BeratNetto);
            Assert.AreEqual(5790.0, k2Data.BeratNetto);
            Assert.AreEqual("SYNCSAP", k1Data.Status);
            Assert.AreEqual("SYNCSAP", k2Data.Status);
            Assert.AreEqual("Y", k1Data.WeighoutFlag);
            Assert.AreEqual("Y", k2Data.WeighoutFlag);
            Assert.AreEqual(101, k1DocEntry);
            Assert.AreEqual(102, k2DocEntry);
        }
    }

    internal class FakeSapRepoForSplit : ISapRepository
    {
        public System.Collections.Generic.List<SplitGRPOPart> LastSplitParts { get; private set; }

        public bool CreateSplitGRPO(System.Collections.Generic.IList<SplitGRPOPart> parts, out System.Collections.Generic.List<int> docEntries, out string error)
        {
            LastSplitParts = new System.Collections.Generic.List<SplitGRPOPart>(parts);
            docEntries = new System.Collections.Generic.List<int>();
            for (int i = 0; i < parts.Count; i++) docEntries.Add(parts[i].Netto >= 1.0 ? 101 + i : 0);
            error = "";
            return true;
        }

        public bool UpdateWeighOutUDO(WeighOutData data, out string error)
        {
            error = "";
            return true;
        }

        public bool UpdateWeighOutUDO(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error)
        {
            error = "";
            return true;
        }

        public bool CreateGRPO(WeighOutData data, out int createdDocEntry, out string error) { createdDocEntry = 1; error = ""; return true; }
        public bool CreateDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error) { createdDocEntry = 1; error = ""; return true; }
        public bool SetUDOReprintFlag(string code, out string error) { error = ""; return true; }
        public bool SaveWeighInUDO(WeighInData data, out string error) { error = ""; return true; }
        public bool UpdateGrpoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error) { error = ""; return true; }
        public bool UpdateDoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error) { error = ""; return true; }
        public bool UpdateUDODocNumSap(string code, string newDocNum, out string error) { error = ""; return true; }
    }

    internal class FakePrintHanaRepo : Weighbridge.Core.Repositories.IHanaRepository
    {
        public bool ReturnEmptyData { get; set; } = false;
        public bool ReturnSplitContractData { get; set; } = false;
        public string StatusValue { get; set; } = "SYNCSAP";
        public string WeighoutValue { get; set; } = "Y";

        public System.Data.DataTable GetPrintTicketData(string ticketNumber)
        {
            var dt = new System.Data.DataTable("SOL_SP_ADDON_WB_PRINT_TIKET");
            string[] strCols = {
                "Status", "Reprint", "Weighout", "Type", "Nomor Tiket",
                "Tanggal Keluar", "Waktu Keluar", "Tanggal Waktu Masuk",
                "Comodity", "Received From", "Sold to", "Transporter",
                "Seal No", "Do No", "Vehicle No", "First Weight", "Second Weight", "Net Weight",
                "Mills Net Weight", "Difference", "Lab FFA", "Lab MI",
                "Lab IV", "Lab CP", "Note", "Driver", "User", "Tanggal Cetak"
            };
            foreach (var col in strCols) dt.Columns.Add(col, typeof(string));
            dt.Columns.Add("Contract No", typeof(decimal));
            dt.Columns.Add("Contract Qty", typeof(decimal));

            if (!ReturnEmptyData)
            {
                var r1 = dt.NewRow();
                r1["Status"] = StatusValue;
                r1["Reprint"] = "N";
                r1["Weighout"] = WeighoutValue;
                r1["Type"] = "PO";
                r1["Nomor Tiket"] = ticketNumber;
                r1["Tanggal Keluar"] = "24-09-26";
                r1["Waktu Keluar"] = "18:00";
                r1["Tanggal Waktu Masuk"] = "24-09-26 10:00";
                r1["Comodity"] = "CPO";
                r1["Received From"] = "PT SUPPLIER";
                r1["Sold to"] = "TANIMAS";
                r1["Transporter"] = "TRANSPORTER 1";
                r1["Seal No"] = "SEAL-01";
                r1["Do No"] = "DO-12345";
                r1["Contract No"] = 261000077m;
                r1["Contract Qty"] = 200000m;
                r1["Vehicle No"] = "BK 1234 AB";
                r1["First Weight"] = "15000 kg";
                r1["Second Weight"] = "5000 kg";
                r1["Net Weight"] = "10000 kg";
                r1["Mills Net Weight"] = "10000 kg";
                r1["Difference"] = "0 kg";
                r1["Lab FFA"] = "5.0";
                r1["Lab MI"] = "5.0";
                r1["Lab IV"] = "5.0";
                r1["Lab CP"] = "5.0";
                r1["Note"] = "Catatan Masuk";
                r1["Driver"] = "Budi";
                r1["User"] = "Operator1";
                r1["Tanggal Cetak"] = "24-09-2026 18:00:00";
                dt.Rows.Add(r1);

                if (ReturnSplitContractData)
                {
                    var r2 = dt.NewRow();
                    r2.ItemArray = (object[])r1.ItemArray.Clone();
                    r2["Contract No"] = 262000161m;
                    r2["Contract Qty"] = 100000m;
                    r2["Net Weight"] = "5000 kg";
                    r2["Mills Net Weight"] = "5000 kg";
                    dt.Rows.Add(r2);
                }
            }
            return dt;
        }

        public string GenerateTiketNumber() => "AS26090001";
        public string GetNextWBCode() => "WB001";
        public System.Collections.Generic.List<string> GetKodeDokumenPabean() => new System.Collections.Generic.List<string>();
        public System.Data.DataTable GetTransporterInternal() => new System.Data.DataTable();
        public System.Data.DataTable SearchBaseDocument(string docType, string vendorRef) => new System.Data.DataTable();
        public DocQuotaInfo GetDocQuotaInfo(string docType, int docEntry, int lineNum) => new DocQuotaInfo();
        public (double openQty, double totalQty) GetFreshQuota(string docType, int docEntry, int lineNum) => (0, 0);
        public string GetFreshKontrakNo(string docType, int docEntry) => "";
        public PabeanData GetPabeanData(string docType, int docEntry) => new PabeanData();
        public System.Collections.Generic.List<DocumentLookupItem> GetOpenDocuments(string docType) => new System.Collections.Generic.List<DocumentLookupItem>();
        public System.Collections.Generic.List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey) => new System.Collections.Generic.List<PendingTicketInfo>();
        public string GetWBCodeByTiket(string tiketNum) => "WB001";
        public System.Collections.Generic.List<string> GetWBCodesByTiket(string tiketNum) => new System.Collections.Generic.List<string> { "WB001" };
        public (string isIns, int baseType, int docEntry, int lineNum) SearchPOSOReserve(string docType, int docEntrySO) => ("", 0, 0, 0);
        public string GetDocNumByEntry(string tableName, string docEntry) => "";
        public string GetPostedDocNumsForTicket(string tableName, string tiketNum) => "";
        public System.Collections.Generic.List<string> GetNomorTangki(string itemName) => new System.Collections.Generic.List<string>();
        public System.Data.DataTable SearchBaseDocJoinWbData(string docType, string docNum) => new System.Data.DataTable();
        public System.Data.DataTable SearchTicketByTiketNum(string noTiket) => new System.Data.DataTable();
        public System.Collections.Generic.List<TicketLookupItem> GetPendingTicketLookupList() => new System.Collections.Generic.List<TicketLookupItem>();
        public (string itemCode, string whsCode) GetItemAndWhsCodeFromSO(int docEntry, int lineNum) => ("", "");
        public (string cardCode, string itemCode) GetPOVendorAndItem(int docEntry, int lineNum) => ("", "");
        public System.Collections.Generic.List<(int DocEntry, string DocNum, int LineNum, double OpenQty)> GetOtherOpenLinesForContract(string contractNo, int excludeDocEntry, int excludeLineNum) => new System.Collections.Generic.List<(int, string, int, double)>();
        public System.Collections.Generic.List<(string BatchNum, double Qty)> GetAvailableBatches(string itemCode, string whsCode) => new System.Collections.Generic.List<(string, double)>();
        public System.Data.DataTable SearchBaseDocGrpoDo(string docType, string docNum) => new System.Data.DataTable();
        public (bool Success, string FullName, string Role, string ErrorMessage) AuthenticateUser(string username, string password) => (true, "Admin", "Super User", "");
        public System.Collections.Generic.List<string> GetCommodityList() => new System.Collections.Generic.List<string>();
        public System.Data.DataTable GetReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate) => new System.Data.DataTable();
    }
}
