using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.ViewModels
{
    public class VerifyOtpVm
    {
        [Required]
        [Display(Name="6-digit OTP")]
        public string Otp { get; set; } = "";
    }
}
