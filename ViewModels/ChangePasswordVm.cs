using System.ComponentModel.DataAnnotations;

namespace HarvestHavenSecurePortal.ViewModels
{
    public class ChangePasswordVm
    {
        [Required, DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = "";
    }
}
