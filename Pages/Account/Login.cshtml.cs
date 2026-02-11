using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Pages.PageHelpers;
using HarvestHavenSecurePortal.Services;
using HarvestHavenSecurePortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HarvestHavenSecurePortal.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly PasswordPolicyService _pwd;
        private readonly CaptchaV3Service _captcha;
        private readonly IConfiguration _config;
        private readonly EmailService _email;
        private readonly TokenService _tokens;

        private const int MaxAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        public LoginModel(AppDbContext db, PasswordPolicyService pwd, CaptchaV3Service captcha, IConfiguration config, EmailService email, TokenService tokens)
        {
            _db = db;
            _pwd = pwd;
            _captcha = captcha;
            _config = config;
            _email = email;
            _tokens = tokens;

            ReCaptchaSiteKey = _config["GoogleReCaptcha:SiteKey"] ?? "";
        }

        [BindProperty]
        public LoginVm Input { get; set; } = new();

        public string? Message { get; set; }

        [BindProperty]
        public string? RecaptchaToken { get; set; }

        public string ReCaptchaSiteKey { get; private set; } = "";

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var (okCaptcha, reason, _) = await _captcha.VerifyAsync(RecaptchaToken ?? "", "login");
            if (!okCaptcha)
            {
                Message = $"reCAPTCHA failed: {reason}";
                SecurityHelpers.AddEvent(_db, Input.Email, "LOGIN_BLOCKED_RECAPTCHA", HttpContext);
                await _db.SaveChangesAsync();
                return Page();
            }

            var member = await _db.MemberProfiles.SingleOrDefaultAsync(m => m.Email == Input.Email);
            if (member is null)
            {
                SecurityHelpers.AddEvent(_db, Input.Email, "LOGIN_FAIL_NOUSER", HttpContext);
                await _db.SaveChangesAsync();
                Message = "Invalid email or password.";
                return Page();
            }

            if (member.LockoutUntil.HasValue && member.LockoutUntil.Value > DateTime.UtcNow)
            {
                SecurityHelpers.AddEvent(_db, member.Email, "LOGIN_BLOCKED_LOCKED", HttpContext);
                await _db.SaveChangesAsync();
                Message = "Account locked. Try again later.";
                return Page();
            }

            if (!_pwd.Verify(member.PasswordHash, Input.Password))
            {
                member.FailedLoginAttempts++;
                SecurityHelpers.AddEvent(_db, member.Email, "LOGIN_FAIL_BADPWD", HttpContext);

                if (member.FailedLoginAttempts >= MaxAttempts)
                {
                    member.LockoutUntil = DateTime.UtcNow.Add(LockoutDuration);
                    member.FailedLoginAttempts = 0;
                    SecurityHelpers.AddEvent(_db, member.Email, "ACCOUNT_LOCKED", HttpContext);
                }

                await _db.SaveChangesAsync();
                Message = "Invalid email or password.";
                return Page();
            }

            // Password correct: clear lockout state
            member.FailedLoginAttempts = 0;
            member.LockoutUntil = null;

            // Generate OTP and email it (2FA)
            var otp = _tokens.GenerateOtp6();
            member.OtpHash = _tokens.Hash(otp);
            member.OtpExpiresAt = DateTime.UtcNow.AddMinutes(5);
            member.OtpAttempts = 0;

            HttpContext.Session.SetInt32("Pending2FAUserId", member.Id);

            SecurityHelpers.AddEvent(_db, member.Email, "2FA_OTP_SENT", HttpContext);
            await _db.SaveChangesAsync();

            await _email.SendAsync(member.Email, "Harvest Haven OTP", $"Your OTP is: {otp}\nIt expires in 5 minutes.");

            return RedirectToPage("/Account/VerifyOtp");
        }
    }
}
