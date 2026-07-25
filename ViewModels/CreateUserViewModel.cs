using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class CreateUserViewModel
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string Role { get; set; } = "User";
    }
}