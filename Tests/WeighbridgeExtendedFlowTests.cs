using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Weighbridge.Core.Models;
using Weighbridge.Core.Services;

namespace Weighbridge.Tests
{
    [TestClass]
    public class WeighbridgeExtendedFlowTests
    {
        // =========================================================================
        // FLOW 1: End-to-End Timbang Masuk (Truk Masuk Dengan Muatan Sawit / CPO)
        // =========================================================================

        [TestMethod]
        public void FlowTimbangMasuk_StandardTBSDelivery_ValidatesAndCalculatesSuccessfully()
        {
            // 1. Raw serial indicator reads Bruto (Truk Berisi Muatan)
            string rawIndicatorBruto = "ST,GS,+032450kg\r\n";
            double bruto = double.Parse(WeighbridgeCalculator.ParseWeight(rawIndicatorBruto));
            Assert.AreEqual(32450.0, bruto, "Bruto harus terbaca 32.450 kg.");

            // 2. Truk keluar nanti akan menimbang Tarra (Truk Kosong). Asumsikan estimasi Tarra
            double estimatedTarra = 11200.0;
            double estimatedNetto = WeighbridgeCalculator.CalculateNetto(bruto, estimatedTarra);
            Assert.AreEqual(21250.0, estimatedNetto, "Netto estimasi harus 21.250 kg.");

            // 3. Validasi Dokumen Pabean (misal impor/kawasan berikat BC 2.3)
            var pabeanResult = PabeanValidator.Validate("BC 2.3", "001234/BC/2026", hasTanggal: true, customDateFormat: "dd/MM/yyyy");
            Assert.IsTrue(pabeanResult.IsValid, "Dokumen Pabean harus valid.");

            // 4. Cek Kuota PO Kontrak (tersedia 25.000 kg)
            double sisaKuotaPO = 25000.0;
            var poQuotaResult = WeighbridgeCalculator.CheckPOQuota(estimatedNetto, sisaKuotaPO, tolerancePercent: 5.0);
            Assert.IsTrue(poQuotaResult.IsValid, "Netto dalam batas kuota PO.");
            Assert.IsFalse(poQuotaResult.IsExceeded, "Tidak melebihi batas kuota.");
            Assert.AreEqual(0, poQuotaResult.ExceedAmount);

            // 5. Cek apakah memerlukan split kontrak
            Assert.IsFalse(WeighbridgeCalculator.RequiresSplitKontrak(estimatedNetto, sisaKuotaPO), "Tidak membutuhkan split kontrak karena kuota mencukupi.");
        }

        // =========================================================================
        // FLOW 2: End-to-End Timbang Masuk + Split Kontrak (Netto Melebihi Kuota K1)
        // =========================================================================

        [TestMethod]
        public void FlowTimbangMasuk_ExceedsSingleContract_SplitsToSecondContractProperly()
        {
            // Truk muatan besar: Bruto 35.000 kg, Tarra 10.000 kg -> Netto 25.000 kg
            double bruto = double.Parse(WeighbridgeCalculator.ParseWeight("+035000.00 kg"));
            double tarra = 10000.0;
            double totalNetto = WeighbridgeCalculator.CalculateNetto(bruto, tarra);
            Assert.AreEqual(25000.0, totalNetto);

            // Kontrak 1 hanya tersisa 15.000 kg
            double sisaKuotaK1 = 15000.0;

            // Hitung split kontrak: supplier menerbitkan SPB K1 15.000 kg sesuai sisa kontrak K1
            Assert.IsTrue(WeighbridgeCalculator.RequiresSplitKontrak(totalNetto, sisaKuotaK1), "Harus split kontrak karena 25.000 kg > 15.000 kg.");
            var splitResult = WeighbridgeCalculator.CalculateSplitBySPB(totalNetto, 15000.0, tarra);
            Assert.IsTrue(splitResult.NeedsSplit);
            Assert.AreEqual(15000.0, splitResult.NettoK1, "Netto K1 = SPB K1 (15.000 kg).");
            Assert.AreEqual(25000.0, splitResult.BrutoK1, "Bruto K1 = Netto K1 + Tarra = 25.000 kg.");
            Assert.AreEqual(10000.0, splitResult.NettoK2, "Netto K2 = Netto total - K1 = 10.000 kg.");
            Assert.AreEqual(20000.0, splitResult.BrutoK2, "Bruto K2 = Netto K2 + Tarra = 20.000 kg.");

            // Validasi sisa kuota K2 (tersedia 20.000 kg di PO kedua)
            double sisaKuotaK2 = 20000.0;
            var k2Validation = WeighbridgeCalculator.ValidateK2Quota(splitResult.NettoK2, sisaKuotaK2);
            Assert.IsTrue(k2Validation.IsValid, "Kuota K2 harus mencukupi.");
            Assert.IsTrue(k2Validation.IsSufficient);
            Assert.AreEqual(10000.0, k2Validation.RemainingQuotaAfterDeduction, "Sisa kuota K2 setelah alokasi = 10.000 kg.");
            Assert.AreEqual(0, k2Validation.Shortage);
        }

        // =========================================================================
        // FLOW 3: End-to-End Timbang Keluar (Penyelesaian Tiket & Rilis GRPO)
        // =========================================================================

        [TestMethod]
        public void FlowTimbangKeluar_NormalWeightOut_CalculatesActualNettoAndGRPO()
        {
            // Tiket Masuk: Bruto = 32.500 kg
            double brutoMasuk = 32500.0;

            // Saat Timbang Keluar: Truk kosong ditimbang (Tarra aktual)
            string rawIndicatorTarra = "ST,GS,+011250kg\r\n";
            double tarraKeluar = double.Parse(WeighbridgeCalculator.ParseWeight(rawIndicatorTarra));
            Assert.AreEqual(11250.0, tarraKeluar);

            // Netto Aktual WB perusahaan
            double actualNettoWB = WeighbridgeCalculator.CalculateNetto(brutoMasuk, tarraKeluar);
            Assert.AreEqual(21250.0, actualNettoWB);

            // Rencana kebun dari Timbang Masuk: K1 = 15.000 kg, K2 = 6.250 kg
            double plannedK1 = 15000.0;
            double plannedK2 = 6250.0;

            // Alokasi Netto WB untuk GRPO Timbang Keluar
            var grpoSplit = WeighbridgeCalculator.CalculateSplitGRPONetto(actualNettoWB, plannedK1, plannedK2);

            Assert.AreEqual(15000.0, grpoSplit.NettoWB_K1, "K1 mengambil hingga kuota kebun K1.");
            Assert.AreEqual(6250.0, grpoSplit.NettoWB_K2, "K2 mengambil seluruh sisa setelah K1.");
            Assert.AreEqual(actualNettoWB, grpoSplit.NettoWB_K1 + grpoSplit.NettoWB_K2, "INVARIANT: K1 + K2 harus sama persis dengan Netto WB.");
            Assert.IsTrue(grpoSplit.ShouldCreateGRPO_K2, "GRPO K2 harus dibuat karena >= 1 kg.");
            Assert.IsFalse(grpoSplit.K2ExceedsPlanned, "K2 tidak melebihi rencana.");
        }

        [TestMethod]
        public void FlowTimbangKeluar_UnderWeightScenario_K2GetsRemainingWithoutNegative()
        {
            // Truk ternyata muatannya lebih sedikit dari perkiraan: Bruto 24.000, Tarra 10.000 -> Netto 14.000 kg
            double bruto = 24000.0;
            double tarra = 10000.0;
            double actualNettoWB = WeighbridgeCalculator.CalculateNetto(bruto, tarra);
            Assert.AreEqual(14000.0, actualNettoWB);

            // Rencana kebun K1 = 15.000 kg, K2 = 5.000 kg
            double plannedK1 = 15000.0;
            double plannedK2 = 5000.0;

            var grpoSplit = WeighbridgeCalculator.CalculateSplitGRPONetto(actualNettoWB, plannedK1, plannedK2);

            Assert.AreEqual(14000.0, grpoSplit.NettoWB_K1, "Semua 14.000 kg diserap oleh K1.");
            Assert.AreEqual(0.0, grpoSplit.NettoWB_K2, "K2 mendapat 0 kg.");
            Assert.IsFalse(grpoSplit.ShouldCreateGRPO_K2, "Tidak boleh membuat GRPO K2 jika 0 kg.");
            Assert.AreEqual(actualNettoWB, grpoSplit.NettoWB_K1 + grpoSplit.NettoWB_K2);
        }

        // =========================================================================
        // FLOW 4: Document Lookup & Search Filter Scenarios
        // =========================================================================

        [TestMethod]
        public void FlowDocumentLookup_ComplexQueries_FiltersAccurately()
        {
            var testItems = new List<DocumentLookupItem>
            {
                new DocumentLookupItem { DocNum = "2026001", CardCode = "V001", CardName = "PT KEBUN MAKMUR", ItemCode = "TBS-01", ItemName = "Tandan Buah Segar", OpenQty = 60000 },
                new DocumentLookupItem { DocNum = "2026002", CardCode = "V002", CardName = "PT SAWIT LESTARI", ItemCode = "CPO-01", ItemName = "Crude Palm Oil", OpenQty = 45000 },
                new DocumentLookupItem { DocNum = "2026003", CardCode = "V003", CardName = "CV MITRA AGRO", ItemCode = "TBS-02", ItemName = "TBS Plasma", OpenQty = 12000 }
            };

            // Cari berdasarkan CardName sebagian
            var res1 = DocumentLookupFilter.Filter(testItems, "lestari");
            Assert.AreEqual(1, res1.Count);
            Assert.AreEqual("2026002", res1[0].DocNum);

            // Cari berdasarkan ItemName
            var res2 = DocumentLookupFilter.Filter(testItems, "crude");
            Assert.AreEqual(1, res2.Count);
            Assert.AreEqual("CPO-01", res2[0].ItemCode);

            // Cari berdasarkan DocNum
            var res3 = DocumentLookupFilter.Filter(testItems, "2026003");
            Assert.AreEqual(1, res3.Count);
            Assert.AreEqual("CV MITRA AGRO", res3[0].CardName);

            // Cari query tidak cocok
            var res4 = DocumentLookupFilter.Filter(testItems, "XYZ999");
            Assert.AreEqual(0, res4.Count);
        }

        // =========================================================================
        // FLOW 5: Weight Parsing Robustness / Dirty Serial Indicator Strings
        // =========================================================================

        [TestMethod]
        public void WeightParsing_ExoticIndicatorFormats_ExtractsCorrectly()
        {
            // Format Avery Weigh-Tronix
            Assert.AreEqual("18420", WeighbridgeCalculator.ParseWeight("\u0002018420 kg\r\u0003"));

            // Format Toledo dengan spasi dan kode status
            Assert.AreEqual("25110", WeighbridgeCalculator.ParseWeight("  25110.00 kg G  "));

            // Format dengan nilai desimal presisi tinggi
            Assert.AreEqual("12345.67", WeighbridgeCalculator.ParseWeight("WT: +12345.67 KG"));

            // Input angka murni tanpa satuan
            Assert.AreEqual("9870", WeighbridgeCalculator.ParseWeight("9870"));
        }
    }
}
