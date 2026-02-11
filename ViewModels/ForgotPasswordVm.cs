using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.ViewModels
{
    public class ForgotPasswordVm
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }
}
