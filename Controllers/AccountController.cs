using FreshFarmSecureApp.Models;
using FreshFarmSecureApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;
using HarvestHavenSecurePortal.Services;

namespace FreshFarmSecureApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryption;
        private readonly EmailService _emailService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly PasswordHasher<Member> _pwHasher;
        private readonly IConfiguration _config;

        public AccountController(ApplicationDbContext context, EncryptionService encryption, EmailService emailService, IHttpContextAccessor httpContext, IConfiguration config)
        {
            _context = context;
            _encryption = encryption;
            _emailService = emailService;
            _httpContext = httpContext;
            _pwHasher = new PasswordHasher<Member>();
            _config = config;
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.RecaptchaSiteKey = _config["ReCaptcha:SiteKey"];
            return View();
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Member model, IFormFile photo, string confirmPassword, string recaptchaToken)
        {
            ViewBag.RecaptchaSiteKey = _config["ReCaptcha:SiteKey"];

            // reCAPTCHA verification - DISABLED for now (keys are invalid)
            // TODO: Update with valid reCAPTCHA v3 keys from Google Console
            /*
            if (string.IsNullOrEmpty(recaptchaToken))
            {
                ModelState.AddModelError("", "reCAPTCHA verification failed");
                return View(model);
            }

            var secretKey = _config["ReCaptcha:SecretKey"];
            using var httpClient = new HttpClient();
            
            try
            {
                var recaptchaResponse = await httpClient.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "secret", secretKey },
                        { "response", recaptchaToken }
                    }));

                var json = await recaptchaResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // Check success flag
                if (!root.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                {
                    ModelState.AddModelError("", "reCAPTCHA verification failed. Please try again.");
                    return View(model);
                }

                // Check score (v3 only) - 0.0 to 1.0, higher is more likely human
                if (root.TryGetProperty("score", out var scoreProp))
                {
                    double score = scoreProp.GetDouble();
                    if (score < 0.5)
                    {
                        ModelState.AddModelError("", "reCAPTCHA detected suspicious activity. Please try again.");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "reCAPTCHA verification error: " + ex.Message);
                return View(model);
            }
            */

            // Input validation/sanitation (anti-XSS, SQLi via EF)
            if (!ModelState.IsValid)
                return View(model);

            // Email unique
            if (_context.Members.Any(m => m.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Duplicate email");
                return View(model);
            }

            // Password match
            if (model.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords don't match");
                return View(model);
            }

            // Password complexity (server-side)
            var pwRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$");
            if (!pwRegex.IsMatch(model.PasswordHash))
            {
                ModelState.AddModelError("PasswordHash", "Password too weak");
                return View(model);
            }

            // Hash password using PasswordHasher
            model.PasswordHash = _pwHasher.HashPassword(model, model.PasswordHash);

            // Encrypt credit card
            model.CreditCardNo = _encryption.Encrypt(model.CreditCardNo);

            // Photo validation
            if (photo != null && photo.ContentType == "image/jpeg" && photo.Length < 5 * 1024 * 1024)
            {
                using var stream = new MemoryStream();
                await photo.CopyToAsync(stream);
                model.Photo = stream.ToArray();
            }
            else if (photo != null)
            {
                ModelState.AddModelError("Photo", "Invalid JPG photo");
                return View(model);
            }

            // AboutMe: Encode to prevent XSS on display
            model.AboutMe = System.Net.WebUtility.HtmlEncode(model.AboutMe);

            // Save
            _context.Members.Add(model);
            await _context.SaveChangesAsync();

            // Audit log
            _context.AuditLogs.Add(new AuditLog { MemberId = model.Id, Action = "Registration", Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Members.FirstOrDefaultAsync(m => m.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid credentials");
                await LogAudit(null, "Login Fail - Invalid Email");
                return View();
            }

            // Lockout check
            if (user.LockoutEnd > DateTime.Now)
            {
                ModelState.AddModelError("", "Account locked");
                return View();
            }

            // Verify password
            var verify = _pwHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verify != PasswordVerificationResult.Success)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 3)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(5);
                }
                await _context.SaveChangesAsync();
                await LogAudit(user.Id, "Login Fail");
                ModelState.AddModelError("", "Invalid credentials");
                return View();
            }

            // Reset attempts
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            // Session management
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetInt32("UserId", user.Id);

            await LogAudit(user.Id, "Login Success");

            return RedirectToAction("Index", "Home");
        }

        // Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            await LogAudit(userId, "Logout");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Change Password (Advanced)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPw, string newPw, string confirmNew)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Members.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            var verify = _pwHasher.VerifyHashedPassword(user, user.PasswordHash, oldPw);
            if (verify != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Current password is incorrect");
                return View();
            }

            if (newPw != confirmNew)
            {
                ModelState.AddModelError("", "New passwords do not match");
                return View();
            }

            var pwRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$");
            if (!pwRegex.IsMatch(newPw))
            {
                ModelState.AddModelError("", "New password does not meet complexity requirements");
                return View();
            }

            user.PasswordHash = _pwHasher.HashPassword(user, newPw);
            await _context.SaveChangesAsync();
            await LogAudit(user.Id, "Change Password");

            return RedirectToAction("Index", "Home");
        }

        // Helper: Log audit actions
        private async Task LogAudit(int? memberId, string action)
        {
            _context.AuditLogs.Add(new AuditLog { MemberId = memberId, Action = action, Timestamp = DateTime.Now });
            await _context.SaveChangesAsync();
        }
    }
}