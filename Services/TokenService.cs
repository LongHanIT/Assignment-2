using System.Security.Cryptography;
using System.Text;

namespace HarvestHavenSecurePortal.Services
{
    public class TokenService
    {
        public string Hash(string token)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        public string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        public string GenerateOtp6()
        {
            // cryptographically strong 6-digit OTP
            var value = RandomNumberGenerator.GetInt32(0, 1000000);
            return value.ToString("D6");
        }
    }
}
