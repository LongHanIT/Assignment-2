using System.ComponentModel.DataAnnotations;

namespace FreshFarmSecureApp.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [CreditCard]  // Built-in validation for credit card format
        public string CreditCardNo { get; set; }  // Store encrypted

        [Required]
        public string Gender { get; set; }

        [Required]
        [Phone]
        [RegularExpression(@"^[0-9]{8}$", ErrorMessage = "8 digits only")]
        public string MobileNo { get; set; }

        [Required]
        public string DeliveryAddress { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }  // Unique via DB index

        public string PasswordHash { get; set; }

        public byte[] Photo { get; set; }  // .JPG as bytes

        public string AboutMe { get; set; }  // Allow special chars

        // Advanced
        public DateTime? LastPasswordChange { get; set; }
        public string PasswordHistory { get; set; }  // Comma-separated last 2 hashes
        public DateTime? LockoutEnd { get; set; }
        public int FailedLoginAttempts { get; set; }
    }
}