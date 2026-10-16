using System.Collections.Generic;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Repositories
{
    public interface ISapRepository
    {
        bool SaveWeighInUDO(WeighInData data, out string error);
        bool UpdateWeighOutUDO(WeighOutData data, out string error);
        bool UpdateWeighOutUDO(string code, double beratKeluar, double beratNetto, string status, string weighout, string tangkiNo, out string error);
        bool CreateGRPO(WeighOutData data, out int createdDocEntry, out string error);
        bool CreateSplitGRPO(IList<SplitGRPOPart> parts, out List<int> docEntries, out string error);
        bool CreateDeliveryOrder(WeighOutData data, string itemCode, string whsCode, out int createdDocEntry, out string error);
        bool SetUDOReprintFlag(string code, out string error);
        bool UpdateGrpoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error);
        bool UpdateDoLineWeight(int docEntry, int lineNum, double qtyNetto, out string error);
    }
}
