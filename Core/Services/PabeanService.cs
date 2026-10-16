using Weighbridge.Core.Models;
using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public interface IPabeanService
    {
        PabeanData GetPabeanData(string docType, int docEntry);
        PabeanValidationResult Validate(string kodeDokumen, string noDokumen, bool hasTanggal, string customDateFormat = "");
        bool HasPabeanRequiredData(PabeanData data);
    }

    public class PabeanService : IPabeanService
    {
        private readonly IHanaRepository _hanaRepo;

        public PabeanService(IHanaRepository hanaRepo)
        {
            _hanaRepo = hanaRepo;
        }

        public PabeanData GetPabeanData(string docType, int docEntry)
        {
            return _hanaRepo.GetPabeanData(docType, docEntry);
        }

        public PabeanValidationResult Validate(string kodeDokumen, string noDokumen, bool hasTanggal, string customDateFormat = "")
        {
            return PabeanValidator.Validate(kodeDokumen, noDokumen, hasTanggal, customDateFormat);
        }

        public bool HasPabeanRequiredData(PabeanData data)
        {
            if (data == null) return false;
            return !string.IsNullOrWhiteSpace(data.KodeDokumen) ||
                   !string.IsNullOrWhiteSpace(data.NomorDokumen) ||
                   data.TanggalDokumen.HasValue;
        }
    }
}
