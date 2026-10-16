using System;

namespace Weighbridge.Core.Models
{
    public class TicketLookupItem
    {
        public string TiketNum { get; set; }
        public string Status { get; set; }
        public string Nopol { get; set; }
        public string NamaSupir { get; set; }
        public string DocType { get; set; }
        public string DocNum { get; set; }
        public string ContractNo { get; set; }
        public string VendorRef { get; set; }
        public string Commodity { get; set; }
        public double BeratMasuk { get; set; }
        public string TanggalMasuk { get; set; }

        public string DisplayText
        {
            get
            {
                string nopolStr = !string.IsNullOrWhiteSpace(Nopol) ? $" | {Nopol}" : "";
                string supirStr = !string.IsNullOrWhiteSpace(NamaSupir) ? $" | {NamaSupir}" : "";
                string docStr = !string.IsNullOrWhiteSpace(DocNum) ? $" | {DocType}: {DocNum}" : "";
                string ktrStr = !string.IsNullOrWhiteSpace(ContractNo) ? $" | Ktr: {ContractNo}" : "";
                string commStr = !string.IsNullOrWhiteSpace(Commodity) ? $" | {Commodity}" : "";
                string statusBadge = string.Equals(Status, "WEIGHIN", StringComparison.OrdinalIgnoreCase) ? "" : $" [{Status}]";

                return $"{TiketNum}{nopolStr}{supirStr}{docStr}{ktrStr}{commStr}{statusBadge}";
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
