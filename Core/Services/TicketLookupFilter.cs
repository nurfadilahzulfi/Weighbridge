using System;
using System.Collections.Generic;
using System.Linq;
using Weighbridge.Core.Models;

namespace Weighbridge.Core.Services
{
    public static class TicketLookupFilter
    {
        public static List<TicketLookupItem> Filter(IEnumerable<TicketLookupItem> items, string query, int maxResults = 25)
        {
            if (items == null) return new List<TicketLookupItem>();
            if (string.IsNullOrWhiteSpace(query)) return new List<TicketLookupItem>();

            string q = query.Trim();

            return items.Where(t =>
                (t.TiketNum != null && t.TiketNum.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.Nopol != null && t.Nopol.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.NamaSupir != null && t.NamaSupir.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.DocNum != null && t.DocNum.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.ContractNo != null && t.ContractNo.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.VendorRef != null && t.VendorRef.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (t.Commodity != null && t.Commodity.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            )
            .OrderByDescending(t => string.Equals(t.Status, "WEIGHIN", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(t => t.TiketNum)
            .Take(maxResults)
            .ToList();
        }
    }
}
