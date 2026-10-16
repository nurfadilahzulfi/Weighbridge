using Weighbridge.Core.Repositories;

namespace Weighbridge.Core.Services
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class UserRoles
    {
        public const string SuperUser = "SU";

        public static bool IsSuperUser(string role)
        {
            return string.Equals(role?.Trim(), SuperUser, System.StringComparison.OrdinalIgnoreCase);
        }

        public static string DisplayName(string role)
        {
            return IsSuperUser(role) ? "Super User" : "Operator";
        }

        public static bool CanAccessConfiguration(string role)
        {
            return IsSuperUser(role);
        }
    }

    public interface IAuthService
    {
        AuthResult Authenticate(string username, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly IHanaRepository _hanaRepo;

        public AuthService() : this(new HanaRepository())
        {
        }

        public AuthService(IHanaRepository hanaRepo)
        {
            _hanaRepo = hanaRepo;
        }

        public AuthResult Authenticate(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Silakan masukkan Username operator."
                };
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Silakan masukkan Kata Sandi."
                };
            }

            var result = _hanaRepo.AuthenticateUser(username, password);
            return new AuthResult
            {
                Success = result.Success,
                FullName = result.FullName,
                Role = result.Role,
                ErrorMessage = result.ErrorMessage
            };
        }
    }
}
