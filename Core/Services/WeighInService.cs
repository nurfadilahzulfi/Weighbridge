using System;
using System.Collections.Generic;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public interface IWeighInService
    {
        string GenerateTicketNumber();
        DocQuotaInfo CheckDocQuota(string docType, int docEntry, int lineNum);
        List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey);
        (double nettoK1, double brutoK1, double nettoK2, double brutoK2) CalculateSplitWeights(double totalNettoSpb, double nettoSpbK1, double taraKebun);
        bool SaveWeighIn(WeighInData data, out string error);
        bool SaveSplitWeighIn(WeighInData k1Data, WeighInData k2Data, out string error);
        bool SaveSplitWeighIn(IList<WeighInData> parts, out string error);
    }

    public class WeighInService : IWeighInService
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly ISapRepository _sapRepo;

        public WeighInService(IHanaRepository hanaRepo, ISapRepository sapRepo)
        {
            _hanaRepo = hanaRepo;
            _sapRepo = sapRepo;
        }

        public string GenerateTicketNumber()
        {
            return _hanaRepo.GenerateTiketNumber();
        }

        public DocQuotaInfo CheckDocQuota(string docType, int docEntry, int lineNum)
        {
            return _hanaRepo.GetDocQuotaInfo(docType, docEntry, lineNum);
        }

        public List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey)
        {
            return _hanaRepo.GetPendingTickets(docType, searchKey);
        }

        public (double nettoK1, double brutoK1, double nettoK2, double brutoK2) CalculateSplitWeights(double totalNettoSpb, double nettoSpbK1, double taraKebun)
        {
            var res = WeighbridgeCalculator.CalculateSplitBySPB(totalNettoSpb, nettoSpbK1, taraKebun);
            return (res.NettoK1, res.BrutoK1, res.NettoK2, res.BrutoK2);
        }

        public bool SaveWeighIn(WeighInData data, out string error)
        {
            if (string.IsNullOrWhiteSpace(data.Code))
            {
                data.Code = _hanaRepo.GetNextWBCode();
            }
            return _sapRepo.SaveWeighInUDO(data, out error);
        }

        public bool SaveSplitWeighIn(WeighInData k1Data, WeighInData k2Data, out string error)
        {
            return SaveSplitWeighIn(new List<WeighInData> { k1Data, k2Data }, out error);
        }

        /// <summary>
        /// Simpan record WB tiap kontrak berurutan (K1, K2, K3). Code dibuat satu per satu sehingga Code K1 &lt; K2 &lt; K3;
        /// urutan Code inilah yang dipakai saat timbang keluar untuk menentukan kontrak terakhir.
        /// </summary>
        public bool SaveSplitWeighIn(IList<WeighInData> parts, out string error)
        {
            error = "";
            if (parts == null || parts.Count < 2)
            {
                error = "Split kontrak membutuhkan minimal 2 kontrak.";
                return false;
            }

            foreach (var data in parts)
            {
                if (string.IsNullOrWhiteSpace(data.Code))
                    data.Code = _hanaRepo.GetNextWBCode();

                if (!_sapRepo.SaveWeighInUDO(data, out error))
                    return false;
            }
            return true;
        }
    }
}
