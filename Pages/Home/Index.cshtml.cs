using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HarvestHavenSecurePortal.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly CryptoService _crypto;

        public IndexModel(AppDbContext db, CryptoService crypto)
        {
            _db = db;
            _crypto = crypto;
        }

        public string FullName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Gender { get; private set; } = "";
        public string PhoneNumber { get; private set; } = "";
        public string DeliveryAddress { get; private set; } = "";
        public string PhotoPath { get; private set; } = "";
        public string? Bio { get; private set; }

        public string DecryptedCard { get; private set; } = "";
        public string MaskedCard { get; private set; } = "";

        public string LastPasswordChangedDisplay { get; private set; } = "—";
        public string SessionIssuedDisplay { get; private set; } = "—";

        public async Task<IActionResult> OnGetAsync()
        {
            var email = HttpContext.Session.GetString("AuthEmail");
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Account/Login");

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Email == email);
            if (member is null || member.SessionStamp != token)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            // Force password change gate
            if (HttpContext.Session.GetString("ForcePasswordChange") == "1")
                return RedirectToPage("/Account/ChangePassword");

            FullName = member.FullName;
            Email = member.Email;
            Gender = member.Gender;
            PhoneNumber = member.PhoneNumber;
            DeliveryAddress = member.DeliveryAddress;
            PhotoPath = member.PhotoPath;
            Bio = member.Bio;

            try
            {
                DecryptedCard = _crypto.Decrypt(member.CardCipher);
                if (DecryptedCard.Length >= 4)
                    MaskedCard = "**** **** **** " + DecryptedCard[^4..];
                else
                    MaskedCard = "****";
            }
            catch
            {
                DecryptedCard = "[Unable to decrypt]";
                MaskedCard = "****";
            }

            if (member.LastPasswordChangedAt.HasValue)
                LastPasswordChangedDisplay = member.LastPasswordChangedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            if (member.SessionIssuedAt.HasValue)
                SessionIssuedDisplay = member.SessionIssuedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

            return Page();
        }
    }
}
