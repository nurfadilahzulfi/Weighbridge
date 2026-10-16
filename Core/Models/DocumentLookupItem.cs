using System;

namespace Weighbridge.Core.Models
{
    public class DocumentLookupItem
    {
        public int DocEntry { get; set; }
        public string DocNum { get; set; }
        public string NumAtCard { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ContractNo { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double OpenQty { get; set; }
        public double Quantity { get; set; }
        public string DocStatus { get; set; }
        public string LineStatus { get; set; }

        public static string FormatKg(double qty)
        {
            if (qty % 1 == 0)
                return ((long)qty).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            else
                return qty.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        }

        public string DisplayText
        {
            get
            {
                string lineInfo = $"Line {LineNum + 1}";
                string quotaInfo = OpenQty > 0 && DocStatus != "C" && LineStatus != "C"
                    ? $"[Kuota: {FormatKg(Quantity)} kg | Sisa: {FormatKg(OpenQty)} kg]"
                    : $"[KUOTA HABIS / 0 kg - CLOSED (Total: {FormatKg(Quantity)} kg)]";

                string ktrStr = !string.IsNullOrEmpty(ContractNo) ? $" | Ktr: {ContractNo}" : "";
                string refStr = !string.IsNullOrEmpty(NumAtCard) && NumAtCard != ContractNo ? $" | Ref: {NumAtCard}" : "";
                string vendorStr = !string.IsNullOrEmpty(CardName) ? $" | {CardName}" : "";
                string itemStr = !string.IsNullOrEmpty(ItemCode) ? $" | {ItemCode}" : "";
                return $"Doc #{DocNum} ({lineInfo}) | {quotaInfo}{ktrStr}{refStr}{vendorStr}{itemStr}";
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}
