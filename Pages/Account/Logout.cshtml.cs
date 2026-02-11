using HarvestHavenSecurePortal.Data;
using HarvestHavenSecurePortal.Pages.PageHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HarvestHavenSecurePortal.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly AppDbContext _db;

        public LogoutModel(AppDbContext db) { _db = db; }

        public IActionResult OnPost()
        {
            var email = HttpContext.Session.GetString("AuthEmail") ?? "";
            if (!string.IsNullOrEmpty(email))
            {
                SecurityHelpers.AddEvent(_db, email, "LOGOUT", HttpContext);
                _db.SaveChanges();
            }

            HttpContext.Session.Clear();
            return RedirectToPage("/Account/Login");
        }
    }
}
