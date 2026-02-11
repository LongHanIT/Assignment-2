using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.Models
{
    public class MemberProfile
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(16)]
        public string Gender { get; set; } = string.Empty;

        [Required, StringLength(24)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DeliveryAddress { get; set; } = string.Empty;

        // Encrypted at rest
        [Required]
        public string CardCipher { get; set; } = string.Empty;

        // Stored file path, e.g. /uploads/abc.jpg
        [Required]
        public string PhotoPath { get; set; } = string.Empty;

        // Stored encoded (to reduce XSS risk)
        public string? Bio { get; set; }

        // Password hash
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Brute force protection
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutUntil { get; set; }

        // Session stamp for multi-login detection
        public string? SessionStamp { get; set; }
        public DateTime? SessionIssuedAt { get; set; }

        // Password age
        public DateTime? LastPasswordChangedAt { get; set; }

        // 2FA OTP (hashed)
        public string? OtpHash { get; set; }
        public DateTime? OtpExpiresAt { get; set; }
        public int OtpAttempts { get; set; } = 0;
    }
}
