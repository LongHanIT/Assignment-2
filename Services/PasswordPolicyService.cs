using Microsoft.AspNetCore.Identity;

namespace HarvestHavenSecurePortal.Services
{
    public class PasswordPolicyService
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string Hash(string password) => _hasher.HashPassword("HarvestHavenMember", password);

        public bool Verify(string hashed, string provided)
            => _hasher.VerifyHashedPassword("HarvestHavenMember", hashed, provided) != PasswordVerificationResult.Failed;

        public (bool ok, string reason) Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required.");

            if (password.Length < 12)
                return (false, "Password must be at least 12 characters.");

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!hasUpper) return (false, "Password must contain at least one uppercase letter.");
            if (!hasLower) return (false, "Password must contain at least one lowercase letter.");
            if (!hasDigit) return (false, "Password must contain at least one number.");
            if (!hasSpecial) return (false, "Password must contain at least one special character.");

            return (true, "OK");
        }
    }
}
