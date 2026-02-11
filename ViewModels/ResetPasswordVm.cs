using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.ViewModels
{
    public class ResetPasswordVm
    {
        [Required]
        public string Token { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
