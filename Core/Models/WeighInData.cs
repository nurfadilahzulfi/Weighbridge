using System;

namespace Weighbridge.Core.Models
{
    public class WeighInData
    {
        public string Code { get; set; } = "";
        public string DocType { get; set; } = "PO";
        public string DocNum { get; set; } = "";
        public string VendorRef { get; set; } = "";
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string TiketNum { get; set; } = "";
        public string Commodity { get; set; } = "";
        public string NoKontrak { get; set; } = "";
        public string Nopol { get; set; } = "";
        public string NamaSupir { get; set; } = "";
        public string NoSimSupir { get; set; } = "";
        public string JenisTruck { get; set; } = "";
        public double BeratMasuk { get; set; }
        public double BrutoKebun { get; set; }
        public double TaraKebun { get; set; }
        public double NettoKebun { get; set; }
        public string FfaKebun { get; set; } = "";
        public string MoistKebun { get; set; } = "";
        public string LabFfa { get; set; } = "";
        public string LabMoist { get; set; } = "";
        public string LabImp { get; set; } = "";
        public string LabDobi { get; set; } = "";
        public string LabCarotine { get; set; } = "";
        public string KodeDokumenPabean { get; set; } = "";
        public string NoDokumenPabean { get; set; } = "";
        public DateTime? TanggalDokumenPabean { get; set; }
        public string NomorSpb { get; set; } = "";
        public DateTime? TanggalSpb { get; set; }
        public string NomorAju { get; set; } = "";
        public string SealNo { get; set; } = "";
        public string DoNo { get; set; } = "";
        public string Container1No { get; set; } = "";
        public string Container2No { get; set; } = "";
        public string Note { get; set; } = "";
        public string Username { get; set; } = "";
        public string Status { get; set; } = "WEIGHIN";
        public string StatusJalur { get; set; } = "";
        public string FasilitasPabean { get; set; } = "";
        public string TransporterInt { get; set; } = "";
        public string TransporterEks { get; set; } = "";

        /// <summary>Kosong = saat ini. Diisi untuk record kontrak lanjutan yang dibuat saat timbang keluar.</summary>
        public DateTime? TanggalMasuk { get; set; }
        public DateTime? WaktuMasuk { get; set; }
    }
}
