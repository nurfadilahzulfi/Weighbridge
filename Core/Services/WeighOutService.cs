using System;
using System.Collections.Generic;
using System.Linq;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public interface IWeighOutService
    {
        double CalculateNetto(double beratMasuk, double beratKeluar);
        bool SyncGRPO(WeighOutData data, out int createdDocEntry, out string error);
        bool SyncSplitGRPO(WeighOutData k1Data, double k1Netto, WeighOutData k2Data, double k2Netto, out int k1DocEntry, out int k2DocEntry, out string error);
        bool SyncSplitGRPO(IList<SplitGRPOPart> parts, out List<int> docEntries, out string error);
        bool SyncDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error);
        bool UpdateUDOStatus(WeighOutData data, out string error);
        bool UpdateUDOStatus(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error);
    }

    public class WeighOutService : IWeighOutService
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;

        public WeighOutService(IHanaRepository hanaRepo, ISapRepository sapRepo)
        {
            _hanaRepo = hanaRepo;
            _sapRepo = sapRepo;
        }

        public double CalculateNetto(double beratMasuk, double beratKeluar)
        {
            return WeighbridgeCalculator.CalculateNetto(beratMasuk, beratKeluar);
        }

        public bool SyncGRPO(WeighOutData data, out int createdDocEntry, out string error)
        {
            createdDocEntry = 0;
            if (!_sapRepo.CreateGRPO(data, out createdDocEntry, out error))
                return false;

            error = MarkUDOSynced(data, $"GRPO (DocEntry {createdDocEntry})");
            return true;
        }

        public bool SyncSplitGRPO(WeighOutData k1Data, double k1Netto, WeighOutData k2Data, double k2Netto, out int k1DocEntry, out int k2DocEntry, out string error)
        {
            var parts = new List<SplitGRPOPart>
            {
                new SplitGRPOPart { Data = k1Data, Netto = k1Netto },
                new SplitGRPOPart { Data = k2Data, Netto = k2Netto }
            };
            bool ok = SyncSplitGRPO(parts, out List<int> docEntries, out error);
            k1DocEntry = docEntries.Count > 0 ? docEntries[0] : 0;
            k2DocEntry = docEntries.Count > 1 ? docEntries[1] : 0;
            return ok;
        }

        public bool SyncSplitGRPO(IList<SplitGRPOPart> parts, out List<int> docEntries, out string error)
        {
            docEntries = new List<int>();

            if (parts == null || parts.Count < 2 || parts.All(p => p.Netto <= 0))
            {
                error = "Netto semua kontrak 0 atau negatif. Tidak dapat membuat GRPO.";
                return false;
            }

            if (!_sapRepo.CreateSplitGRPO(parts, out docEntries, out error))
                return false;

            var warnings = new List<string>();
            for (int i = 0; i < parts.Count; i++)
            {
                int docEntry = i < docEntries.Count ? docEntries[i] : 0;
                parts[i].Data.BeratNetto = parts[i].Netto;
                string warn = MarkUDOSynced(parts[i].Data, $"GRPO K{i + 1} (DocEntry {docEntry})", resolveCodeByTiket: false);
                if (!string.IsNullOrEmpty(warn)) warnings.Add(warn);
            }

            error = string.Join("\n", warnings);
            return true;
        }

        public bool SyncDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error)
        {
            createdDocEntry = 0;
            if (!_sapRepo.CreateDeliveryOrder(data, itemCode, whsCode, out createdDocEntry, out error))
                return false;

            error = MarkUDOSynced(data, $"Delivery Order (DocEntry {createdDocEntry})");
            return true;
        }

        private string MarkUDOSynced(WeighOutData data, string docLabel, bool resolveCodeByTiket = true)
        {
            string udoCode = !string.IsNullOrWhiteSpace(data.Code)
                ? data.Code
                : (resolveCodeByTiket ? _hanaRepo.GetWBCodeByTiket(data.TiketNum) : "");

            if (string.IsNullOrWhiteSpace(udoCode))
            {
                return $"{docLabel} sudah dibuat, tetapi record Weighbridge (UDO) tiket {data.TiketNum} tidak ditemukan sehingga status tidak diubah ke SYNCSAP. " +
                       "JANGAN proses ulang tiket ini; hubungi admin SAP.";
            }

            data.Code = udoCode;
            data.Status = "SYNCSAP";
            data.WeighoutFlag = "Y";
            if (!_sapRepo.UpdateWeighOutUDO(data, out string udoError))
            {
                return $"{docLabel} sudah dibuat, tetapi update status Weighbridge (UDO {udoCode}) gagal: {udoError}. " +
                       "JANGAN proses ulang tiket ini; hubungi admin SAP.";
            }
            return "";
        }

        public bool UpdateUDOStatus(WeighOutData data, out string error)
        {
            return _sapRepo.UpdateWeighOutUDO(data, out error);
        }

        public bool UpdateUDOStatus(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error)
        {
            return _sapRepo.UpdateWeighOutUDO(code, beratKeluar, beratNetto, status, weighout, tangkiNo, out error);
        }
    }
}
