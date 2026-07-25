using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;


    }
}