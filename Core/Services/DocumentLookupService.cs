using System;
using System.Collections.Generic;
using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public interface IDocumentLookupService
    {
        List<DocumentLookupItem> GetOpenDocuments(string docType, bool forceRefresh = false);
        List<DocumentLookupItem> Search(string docType, string query, int maxResults = 20);
        List<TicketLookupItem> SearchTickets(IEnumerable<TicketLookupItem> sourceTickets, string query, int maxResults = 20);
    }

    public class DocumentLookupService : IDocumentLookupService
    {
        private readonly IHanaRepository _hanaRepo;
        private readonly Dictionary<string, List<DocumentLookupItem>> _cache = new Dictionary<string, List<DocumentLookupItem>>(StringComparer.OrdinalIgnoreCase);

        public DocumentLookupService(IHanaRepository hanaRepo)
        {
            _hanaRepo = hanaRepo;
        }

        public List<DocumentLookupItem> GetOpenDocuments(string docType, bool forceRefresh = false)
        {
            if (string.IsNullOrWhiteSpace(docType)) docType = "PO";

            if (!forceRefresh && _cache.TryGetValue(docType, out var cachedList) && cachedList != null && cachedList.Count > 0)
            {
                return cachedList;
            }

            var freshList = _hanaRepo.GetOpenDocuments(docType);
            _cache[docType] = freshList;
            return freshList;
        }

        public List<DocumentLookupItem> Search(string docType, string query, int maxResults = 20)
        {
            var openDocs = GetOpenDocuments(docType, forceRefresh: false);
            return DocumentLookupFilter.Filter(openDocs, query, maxResults);
        }

        public List<TicketLookupItem> SearchTickets(IEnumerable<TicketLookupItem> sourceTickets, string query, int maxResults = 20)
        {
            return TicketLookupFilter.Filter(sourceTickets, query, maxResults);
        }
    }
}
