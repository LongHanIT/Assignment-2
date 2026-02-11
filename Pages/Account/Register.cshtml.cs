using System.Net;
using System.Text.Encodings.Web;
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
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly CryptoService _crypto;
        private readonly PasswordPolicyService _pwd;
        private readonly HtmlEncoder _encoder;

        public RegisterModel(AppDbContext db, CryptoService crypto, PasswordPolicyService pwd, HtmlEncoder encoder)
        {
            _db = db;
            _crypto = crypto;
            _pwd = pwd;
            _encoder = encoder;
        }

        [BindProperty]
        public RegisterVm Input { get; set; } = new();

        public string? Message { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            // Duplicate email check
            var exists = await _db.MemberProfiles.AnyAsync(m => m.Email == Input.Email);
            if (exists)
            {
                ModelState.AddModelError(nameof(Input.Email), "Email already exists.");
                return Page();
            }

            // Password policy
            var pwCheck = _pwd.Validate(Input.Password);
            if (!pwCheck.ok)
            {
                ModelState.AddModelError(nameof(Input.Password), pwCheck.reason);
                return Page();
            }

            // File upload restriction: JPG only
            if (Input.Photo == null || Input.Photo.Length == 0)
            {
                ModelState.AddModelError(nameof(Input.Photo), "Photo is required.");
                return Page();
            }

            var ext = Path.GetExtension(Input.Photo.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg")
            {
                ModelState.AddModelError(nameof(Input.Photo), "Only .jpg/.jpeg files are allowed.");
                return Page();
            }

            if (!string.Equals(Input.Photo.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(Input.Photo), "Only JPEG images are allowed.");
                return Page();
            }

            // Save photo
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid():N}.jpg";
            var savePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await Input.Photo.CopyToAsync(stream);
            }

            var photoPath = $"/uploads/{fileName}";

            // Encode bio before storing
            var safeBio = string.IsNullOrWhiteSpace(Input.Bio) ? null : _encoder.Encode(Input.Bio);

            // Encrypt card
            var cardCipher = _crypto.Encrypt(Input.CardNumber);

            var member = new MemberProfile
            {
                FullName = Input.FullName,
                Email = Input.Email,
                Gender = Input.Gender,
                PhoneNumber = Input.PhoneNumber,
                DeliveryAddress = Input.DeliveryAddress,
                CardCipher = cardCipher,
                PhotoPath = photoPath,
                Bio = safeBio,
                PasswordHash = _pwd.Hash(Input.Password),
                LastPasswordChangedAt = DateTime.UtcNow
            };

            _db.MemberProfiles.Add(member);
            SecurityHelpers.AddEvent(_db, member.Email, "REGISTER_SUCCESS", HttpContext);
            await _db.SaveChangesAsync();

            TempData["Info"] = "Account created. Please sign in.";
            return RedirectToPage("/Account/Login");
        }
    }
}
