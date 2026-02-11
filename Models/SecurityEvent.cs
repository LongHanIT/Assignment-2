using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.Models
{
    public class SecurityEvent
    {
        public int Id { get; set; }

        [Required, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(64)]
        public string Action { get; set; } = string.Empty;

        [StringLength(64)]
        public string? IpAddress { get; set; }

        [StringLength(256)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
