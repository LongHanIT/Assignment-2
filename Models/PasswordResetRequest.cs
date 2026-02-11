using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.Models
{
    public class PasswordResetRequest
    {
        public int Id { get; set; }

        [Required]
        public int MemberProfileId { get; set; }

        // store HASH of token, not token itself
        [Required]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
