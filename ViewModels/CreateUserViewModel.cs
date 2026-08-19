using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Please select a role.")]
        public string Role { get; set; } = "";
    }
}