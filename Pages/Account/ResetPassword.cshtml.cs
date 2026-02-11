using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Models;
using HarvestHavenSecurePortal.Pages.PageHelpers;
using HarvestHavenSecurePortal.Services;
using HarvestHavenSecurePortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HarvestHavenSecurePortal.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly PasswordPolicyService _pwd;
        private readonly TokenService _tokens;

        public ResetPasswordModel(AppDbContext db, PasswordPolicyService pwd, TokenService tokens)
        {
            _db = db;
            _pwd = pwd;
            _tokens = tokens;
        }

        [BindProperty]
        public ResetPasswordVm Input { get; set; } = new();

        public string? Error { get; set; }

        public IActionResult OnGet(string token)
        {
            Input.Token = token;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var check = _pwd.Validate(Input.NewPassword);
            if (!check.ok)
            {
                ModelState.AddModelError(nameof(Input.NewPassword), check.reason);
                return Page();
            }

            var tokenHash = _tokens.Hash(Input.Token);

            var reset = await _db.PasswordResetRequests
                .Where(r => r.TokenHash == tokenHash)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            if (reset == null || reset.UsedAt != null || reset.ExpiresAt < DateTime.UtcNow)
            {
                Error = "Reset link is invalid or expired.";
                return Page();
            }

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Id == reset.MemberProfileId);
            if (member == null)
            {
                Error = "Reset link is invalid or expired.";
                return Page();
            }

            // Invalidate sessions & OTP
            member.SessionStamp = null;
            member.SessionIssuedAt = null;
            member.OtpHash = null;
            member.OtpExpiresAt = null;
            member.OtpAttempts = 0;

            // Archive current password
            _db.PasswordArchives.Add(new PasswordArchive
            {
                MemberProfileId = member.Id,
                PasswordHash = member.PasswordHash,
                CreatedAt = DateTime.UtcNow
            });

            member.PasswordHash = _pwd.Hash(Input.NewPassword);
            member.LastPasswordChangedAt = DateTime.UtcNow;

            reset.UsedAt = DateTime.UtcNow;

            SecurityHelpers.AddEvent(_db, member.Email, "PW_RESET_SUCCESS", HttpContext);

            await _db.SaveChangesAsync();

            TempData["Info"] = "Password reset successful. Please sign in.";
            return RedirectToPage("/Account/Login");
        }
    }
}
