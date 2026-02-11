using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace HarvestHavenSecurePortal.ViewModels
{
    public class RegisterVm
    {
        [Required, StringLength(80)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Gender { get; set; } = "";

        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string DeliveryAddress { get; set; } = "";

        [Required]
        [Display(Name="Card Number")]
        public string CardNumber { get; set; } = "";

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        [Required]
        public IFormFile Photo { get; set; } = default!;

        public string? Bio { get; set; }
    }
}
