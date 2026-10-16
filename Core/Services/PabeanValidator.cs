using System;

namespace Weighbridge.Core.Services
{
    public class PabeanValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class PabeanValidator
    {
        public static PabeanValidationResult Validate(string kodeDokumen, string noDokumen, bool hasTanggal, string customDateFormat = "")
        {
            if (string.IsNullOrWhiteSpace(kodeDokumen))
            {
                return new PabeanValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Kode Dokumen Pabean wajib diisi!"
                };
            }

            if (string.IsNullOrWhiteSpace(noDokumen))
            {
                return new PabeanValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Nomor Dokumen Pabean wajib diisi!"
                };
            }
            if (!hasTanggal || customDateFormat == " ")
            {
                return new PabeanValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Tanggal Dokumen Pabean wajib diisi!"
                };
            }

            return new PabeanValidationResult
            {
                IsValid = true,
                ErrorMessage = string.Empty
            };
        }
    }
}
