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
    public class ForgotPasswordModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly EmailService _email;
        private readonly TokenService _tokens;

        public ForgotPasswordModel(AppDbContext db, EmailService email, TokenService tokens)
        {
            _db = db;
            _email = email;
            _tokens = tokens;
        }

        [BindProperty]
        public ForgotPasswordVm Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Always generic message to avoid user enumeration
            TempData["Info"] = "If the email exists, a reset link has been sent.";

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Email == Input.Email);
            if (member == null)
            {
                SecurityHelpers.AddEvent(_db, Input.Email, "PW_RESET_REQUEST_NOUSER", HttpContext);
                await _db.SaveChangesAsync();
                return RedirectToPage();
            }

            var token = _tokens.GenerateSecureToken();
            var tokenHash = _tokens.Hash(token);

            _db.PasswordResetRequests.Add(new PasswordResetRequest
            {
                MemberProfileId = member.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                UsedAt = null
            });

            SecurityHelpers.AddEvent(_db, member.Email, "PW_RESET_REQUESTED", HttpContext);
            await _db.SaveChangesAsync();

            var link = Url.Page("/Account/ResetPassword", null, new { token }, Request.Scheme);
            await _email.SendAsync(member.Email, "Harvest Haven - Password Reset",
                $"Click to reset your password (valid 15 minutes):\n{link}");

            return RedirectToPage();
        }
    }
}
