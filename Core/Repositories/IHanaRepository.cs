using System.Collections.Generic;
using System.Data;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Repositories
{
    public interface IHanaRepository
    {
        string GenerateTiketNumber();
        string GetNextWBCode();
        List<string> GetKodeDokumenPabean();
        DataTable GetTransporterInternal();
        DataTable SearchBaseDocument(string docType, string vendorRef);
        DocQuotaInfo GetDocQuotaInfo(string docType, int docEntry, int lineNum);
        (double openQty, double totalQty) GetFreshQuota(string docType, int docEntry, int lineNum);
        string GetFreshKontrakNo(string docType, int docEntry);
        PabeanData GetPabeanData(string docType, int docEntry);
        List<DocumentLookupItem> GetOpenDocuments(string docType);
        List<PendingTicketInfo> GetPendingTickets(string docType, string searchKey);
        string GetWBCodeByTiket(string tiketNum);
        List<string> GetWBCodesByTiket(string tiketNum);
        (string isIns, int baseType, int docEntry, int lineNum) SearchPOSOReserve(string docType, int docEntrySO);
        string GetDocNumByEntry(string tableName, string docEntry);
        string GetPostedDocNumsForTicket(string tableName, string tiketNum);
        List<string> GetNomorTangki(string itemName);
        DataTable SearchBaseDocJoinWbData(string docType, string docNum);
        DataTable SearchTicketByTiketNum(string noTiket);
        List<TicketLookupItem> GetPendingTicketLookupList();
        (string itemCode, string whsCode) GetItemAndWhsCodeFromSO(int docEntry, int lineNum);
        (string cardCode, string itemCode) GetPOVendorAndItem(int docEntry, int lineNum);
        List<(int DocEntry, string DocNum, int LineNum, double OpenQty)> GetOtherOpenLinesForContract(string contractNo, int excludeDocEntry, int excludeLineNum);
        List<(string BatchNum, double Qty)> GetAvailableBatches(string itemCode, string whsCode);
        DataTable SearchBaseDocGrpoDo(string docType, string docNum);
        (bool Success, string FullName, string Role, string ErrorMessage) AuthenticateUser(string username, string password);
        List<string> GetCommodityList();
        DataTable GetReportData(string noTiket, string noPolisi, string commodity, string fromDate, string toDate);
        DataTable GetPrintTicketData(string ticketNumber);
    }
}
