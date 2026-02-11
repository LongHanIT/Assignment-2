using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Models;

namespace HarvestHavenSecurePortal.Pages.PageHelpers
{
    public static class SecurityHelpers
    {
        public static void AddEvent(AppDbContext db, string email, string action, HttpContext ctx)
        {
            db.SecurityEvents.Add(new SecurityEvent
            {
                Email = email,
                Action = action,
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent = ctx.Request.Headers.UserAgent.ToString()
            });
        }
    }
}
