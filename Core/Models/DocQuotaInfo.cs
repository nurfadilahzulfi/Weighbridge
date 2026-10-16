namespace Weighbridge.Core.Models
{
    public class DocQuotaInfo
    {
        public string HeaderStatus { get; set; } = "";
        public string LineStatus { get; set; } = "";
        public double OpenQty { get; set; }
        public double TotalQty { get; set; }

        public bool IsClosed => HeaderStatus == "C" || LineStatus == "C";
    }
}
