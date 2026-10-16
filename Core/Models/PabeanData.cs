using System;

namespace Weighbridge.Core.Models
{
    public class PabeanData
    {
        public string NomorSPB { get; set; } = "";
        public DateTime? TanggalSPB { get; set; }
        public string StatusJalur { get; set; } = "";
        public string FasilitasPabean { get; set; } = "";
        public string KodeDokumen { get; set; } = "";
        public string NomorDokumen { get; set; } = "";
        public DateTime? TanggalDokumen { get; set; }
        public string NomorAju { get; set; } = "";
        public string NoKontrak { get; set; } = "";
    }
}
