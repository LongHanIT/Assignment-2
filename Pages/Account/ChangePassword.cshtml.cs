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
    public class ChangePasswordModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly PasswordPolicyService _pwd;
        private readonly IConfiguration _config;

        public ChangePasswordModel(AppDbContext db, PasswordPolicyService pwd, IConfiguration config)
        {
            _db = db;
            _pwd = pwd;
            _config = config;
        }

        [BindProperty]
        public ChangePasswordVm Input { get; set; } = new();

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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var email = HttpContext.Session.GetString("AuthEmail");
            var token = HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
                return Page();

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Email == email);
            if (member is null || member.SessionStamp != token)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Account/Login");
            }

            int minAgeMinutes = _config.GetValue<int>("Security:MinPasswordAgeMinutes", 1);
            if (member.LastPasswordChangedAt.HasValue &&
                DateTime.UtcNow - member.LastPasswordChangedAt.Value < TimeSpan.FromMinutes(minAgeMinutes))
            {
                ModelState.AddModelError(string.Empty, $"You can only change password once every {minAgeMinutes} minute(s).");
                return Page();
            }

            if (!_pwd.Verify(member.PasswordHash, Input.CurrentPassword))
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                SecurityHelpers.AddEvent(_db, member.Email, "CHANGE_PASSWORD_FAIL_BAD_CURRENT", HttpContext);
                await _db.SaveChangesAsync();
                return Page();
            }

            var check = _pwd.Validate(Input.NewPassword);
            if (!check.ok)
            {
                ModelState.AddModelError(nameof(Input.NewPassword), check.reason);
                return Page();
            }

            // Prevent reuse: current + last 2
            var recent = await _db.PasswordArchives
                .Where(p => p.MemberProfileId == member.Id)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => p.PasswordHash)
                .Take(2)
                .ToListAsync();

            recent.Insert(0, member.PasswordHash);

            foreach (var oldHash in recent)
            {
                if (_pwd.Verify(oldHash, Input.NewPassword))
                {
                    ModelState.AddModelError(string.Empty, "You cannot reuse your recent passwords.");
                    SecurityHelpers.AddEvent(_db, member.Email, "CHANGE_PASSWORD_FAIL_REUSE", HttpContext);
                    await _db.SaveChangesAsync();
                    return Page();
                }
            }

            // Archive current hash
            _db.PasswordArchives.Add(new PasswordArchive
            {
                MemberProfileId = member.Id,
                PasswordHash = member.PasswordHash,
                CreatedAt = DateTime.UtcNow
            });

            member.PasswordHash = _pwd.Hash(Input.NewPassword);
            member.LastPasswordChangedAt = DateTime.UtcNow;

            HttpContext.Session.Remove("ForcePasswordChange");

            SecurityHelpers.AddEvent(_db, member.Email, "CHANGE_PASSWORD_SUCCESS", HttpContext);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToPage("/Home/Index");
        }
    }
}
