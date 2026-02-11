using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.Models
{
    public class PasswordArchive
    {
        public int Id { get; set; }

        [Required]
        public int MemberProfileId { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
