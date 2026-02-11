namespace FreshFarmSecureApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? MemberId { get; set; }
        public string? Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
    }
}