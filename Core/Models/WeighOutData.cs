using System;

namespace Weighbridge.Core.Models
{
    public class WeighOutData
    {
        public string TiketNum { get; set; } = "";
        public string Code { get; set; } = "";
        public string DocType { get; set; } = "PO";
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string DocNum { get; set; } = "";
        public string NoKontrak { get; set; } = "";
        public string TangkiNo { get; set; } = "";
        public double BeratMasuk { get; set; }
        public double BeratKeluar { get; set; }
        public double BeratNetto { get; set; }
        public string Status { get; set; } = "WEIGHOUT";
        public string WeighoutFlag { get; set; } = "Y";

        public string KodeDokumenPabean { get; set; } = "";
        public string NoDokumenPabean { get; set; } = "";
        public DateTime? TanggalDokumenPabean { get; set; }
        public string NomorSpb { get; set; } = "";
        public string NomorAju { get; set; } = "";

        public string NamaSupir { get; set; } = "";
        public string JenisTruck { get; set; } = "";
        public string Nopol { get; set; } = "";
        public string TransporterInt { get; set; } = "";
        public string TransporterEks { get; set; } = "";

        public string BrutoKebun { get; set; } = "";
        public string TaraKebun { get; set; } = "";
        public string NettoKebun { get; set; } = "";
        public string FfaKebun { get; set; } = "";
        public string MoistKebun { get; set; } = "";

        public string LabFfa { get; set; } = "";
        public string LabMoist { get; set; } = "";
        public string LabImp { get; set; } = "";
        public string LabDobi { get; set; } = "";
        public string LabCarotine { get; set; } = "";

        public string SealNo { get; set; } = "";
        public string DoNo { get; set; } = "";
        public string Note { get; set; } = "";
        public string Container1No { get; set; } = "";
        public string Container2No { get; set; } = "";
    }

    /// <summary>Satu bagian GRPO tiket split kontrak (K1, K2, K3) beserta netto alokasinya.</summary>
    public class SplitGRPOPart
    {
        public WeighOutData Data { get; set; }
        public double Netto { get; set; }
    }
}
