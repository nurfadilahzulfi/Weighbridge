using System;
using System.Collections.Generic;
using System.Linq;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Services
{
    public static class DocumentLookupFilter
    {
        public static List<DocumentLookupItem> Filter(IEnumerable<DocumentLookupItem> items, string query, int maxResults = 20)
        {
            if (items == null) return new List<DocumentLookupItem>();
            if (string.IsNullOrWhiteSpace(query)) return new List<DocumentLookupItem>();

            string q = query.Trim();

            return items.Where(d =>
                (d.DocNum != null && d.DocNum.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.NumAtCard != null && d.NumAtCard.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.CardName != null && d.CardName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.CardCode != null && d.CardCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.ContractNo != null && d.ContractNo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.ItemCode != null && d.ItemCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (d.ItemName != null && d.ItemName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            )
            .OrderByDescending(d => d.OpenQty > 0)
            .ThenByDescending(d => d.DocNum)
            .ThenBy(d => d.LineNum)
            .Take(maxResults)
            .ToList();
        }

        // No. dokumen atau Vendor Ref yang sama persis; baris dengan sisa qty didahulukan
        public static DocumentLookupItem FindExact(IEnumerable<DocumentLookupItem> items, string docNumOrRef)
        {
            if (items == null || string.IsNullOrWhiteSpace(docNumOrRef)) return null;
            string q = docNumOrRef.Trim();

            return items
                .Where(d => string.Equals(d.DocNum?.Trim(), q, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(d.NumAtCard?.Trim(), q, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.OpenQty > 0)
                .ThenBy(d => d.LineNum)
                .FirstOrDefault();
        }

        public const double MinimumPOQuotaKg = 55000.0;

        public static bool CanProceedWithQuota(DocumentLookupItem item, out string rejectionMessage)
        {
            return CanProceedWithQuota(item, out rejectionMessage, 0);
        }

        public static bool CanProceedWithQuota(DocumentLookupItem item, out string rejectionMessage, double minimumQuotaKg)
        {
            if (item == null)
            {
                rejectionMessage = "Dokumen tidak valid.";
                return false;
            }

            rejectionMessage = string.Empty;
            return true;
        }
    }
}
