using Microsoft.AspNetCore.DataProtection;

namespace HarvestHavenSecurePortal.Services
{
    public class CryptoService
    {
        private readonly IDataProtector _protector;

        public CryptoService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("HarvestHaven.Card.v1");
        }

        public string Encrypt(string plaintext) => _protector.Protect(plaintext);
        public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
    }
}
