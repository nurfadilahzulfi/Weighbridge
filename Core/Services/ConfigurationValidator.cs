namespace Weighbridge.Core.Services
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Ok() => new ValidationResult { IsValid = true, ErrorMessage = "" };
        public static ValidationResult Fail(string msg) => new ValidationResult { IsValid = false, ErrorMessage = msg };
    }

    public interface IConfigurationValidator
    {
        ValidationResult ValidateWbConfig(string port, string baudRate, string dataBits, string readTimeout, string writeTimeout);
        ValidationResult ValidateSboConfig(string srvType, string dbSrv, string dbSbo, string dbUser, string dbPass, string sboUser, string sboPass);
    }

    public class ConfigurationValidator : IConfigurationValidator
    {
        public ValidationResult ValidateWbConfig(string port, string baudRate, string dataBits, string readTimeout, string writeTimeout)
        {
            if (string.IsNullOrWhiteSpace(port))
                return ValidationResult.Fail("Weighbridge Port Cannot be Empty");
            if (string.IsNullOrWhiteSpace(baudRate))
                return ValidationResult.Fail("Weighbridge Baud Rate Cannot be Empty");
            if (string.IsNullOrWhiteSpace(dataBits))
                return ValidationResult.Fail("Weighbridge Data Bits Cannot be Empty");
            if (string.IsNullOrWhiteSpace(readTimeout))
                return ValidationResult.Fail("Weighbridge Read Timeout Cannot be Empty");
            if (string.IsNullOrWhiteSpace(writeTimeout))
                return ValidationResult.Fail("Weighbridge Write Timeout Cannot be Empty");

            return ValidationResult.Ok();
        }

        public ValidationResult ValidateSboConfig(string srvType, string dbSrv, string dbSbo, string dbUser, string dbPass, string sboUser, string sboPass)
        {
            if (string.IsNullOrWhiteSpace(srvType))
                return ValidationResult.Fail("SBO Database Server Type Cannot be Empty");
            if (string.IsNullOrWhiteSpace(dbSrv))
                return ValidationResult.Fail("SBO Database Server Address Cannot be Empty");
            if (string.IsNullOrWhiteSpace(dbSbo))
                return ValidationResult.Fail("SBO Company Database Cannot be Empty");
            if (string.IsNullOrWhiteSpace(dbUser))
                return ValidationResult.Fail("SBO Company Database Username Cannot be Empty");
            if (string.IsNullOrWhiteSpace(dbPass))
                return ValidationResult.Fail("SBO Company Database Password Cannot be Empty");
            if (string.IsNullOrWhiteSpace(sboUser))
                return ValidationResult.Fail("SBO Company User Name Cannot be Empty");
            if (string.IsNullOrWhiteSpace(sboPass))
                return ValidationResult.Fail("SBO Company User Password Cannot be Empty");

            return ValidationResult.Ok();
        }
    }
}
