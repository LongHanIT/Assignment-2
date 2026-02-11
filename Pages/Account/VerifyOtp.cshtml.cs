using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Pages.PageHelpers;
using HarvestHavenSecurePortal.Services;
using HarvestHavenSecurePortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HarvestHavenSecurePortal.Pages.Account
{
    public class VerifyOtpModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly TokenService _tokens;
        private readonly IConfiguration _config;

        public VerifyOtpModel(AppDbContext db, TokenService tokens, IConfiguration config)
        {
            _db = db;
            _tokens = tokens;
            _config = config;
        }

        [BindProperty]
        public VerifyOtpVm Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("Pending2FAUserId");
            if (userId == null)
            {
                TempData["Info"] = "2FA session expired. Please sign in again.";
                return RedirectToPage("/Account/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var userId = HttpContext.Session.GetInt32("Pending2FAUserId");
            if (userId == null)
            {
                TempData["Info"] = "2FA session expired. Please sign in again.";
                return RedirectToPage("/Account/Login");
            }

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Id == userId.Value);
            if (member == null)
            {
                HttpContext.Session.Remove("Pending2FAUserId");
                TempData["Info"] = "Account not found. Please sign in again.";
                return RedirectToPage("/Account/Login");
            }

            if (member.OtpExpiresAt == null || member.OtpExpiresAt < DateTime.UtcNow)
            {
                HttpContext.Session.Remove("Pending2FAUserId");
                member.OtpHash = null;
                member.OtpExpiresAt = null;
                member.OtpAttempts = 0;
                SecurityHelpers.AddEvent(_db, member.Email, "2FA_OTP_EXPIRED", HttpContext);
                await _db.SaveChangesAsync();

                TempData["Info"] = "OTP expired. Please sign in again.";
                return RedirectToPage("/Account/Login");
            }

            member.OtpAttempts++;
            if (member.OtpAttempts > 5)
            {
                HttpContext.Session.Remove("Pending2FAUserId");
                member.OtpHash = null;
                member.OtpExpiresAt = null;
                member.OtpAttempts = 0;

                SecurityHelpers.AddEvent(_db, member.Email, "2FA_OTP_TOO_MANY", HttpContext);
                await _db.SaveChangesAsync();

                TempData["Info"] = "Too many OTP attempts. Please sign in again.";
                return RedirectToPage("/Account/Login");
            }

            var providedHash = _tokens.Hash(Input.Otp);
            if (member.OtpHash != providedHash)
            {
                SecurityHelpers.AddEvent(_db, member.Email, "2FA_OTP_FAIL", HttpContext);
                await _db.SaveChangesAsync();

                ModelState.AddModelError(string.Empty, "Invalid OTP.");
                return Page();
            }

            // OTP success: clear OTP fields
            member.OtpHash = null;
            member.OtpExpiresAt = null;
            member.OtpAttempts = 0;

            // Max password age check (AFTER 2FA)
            int maxAgeDays = _config.GetValue<int>("Security:MaxPasswordAgeDays", 90);
            if (maxAgeDays < 1) maxAgeDays = 1; // prevents demo loops when set to 0

            var lastChanged = member.LastPasswordChangedAt ?? DateTime.MinValue;
            var age = DateTime.UtcNow - lastChanged;

            if (age > TimeSpan.FromDays(maxAgeDays))
            {
                HttpContext.Session.SetString("ForcePasswordChange", "1");
                TempData["Info"] = $"Your password is older than {maxAgeDays} days. Please change it to continue.";
            }

            // Complete login session (multi-login detection)
            var sessionToken = Guid.NewGuid().ToString("N");
            member.SessionStamp = sessionToken;
            member.SessionIssuedAt = DateTime.UtcNow;

            HttpContext.Session.Remove("Pending2FAUserId");
            HttpContext.Session.SetString("AuthEmail", member.Email);
            HttpContext.Session.SetString("AuthToken", sessionToken);
            HttpContext.Session.SetInt32("AuthUserId", member.Id);

            SecurityHelpers.AddEvent(_db, member.Email, "LOGIN_SUCCESS_2FA", HttpContext);
            await _db.SaveChangesAsync();

            if (HttpContext.Session.GetString("ForcePasswordChange") == "1")
                return RedirectToPage("/Account/ChangePassword");

            return RedirectToPage("/Home/Index");
        }
    }
}
