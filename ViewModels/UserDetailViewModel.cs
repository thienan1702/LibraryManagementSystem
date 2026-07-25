using LibraryManagement.Models;

namespace LibraryManagement.ViewModels
{
    public class UserDetailViewModel
    {
        public ApplicationUser User { get; set; }

        public string Role { get; set; }

        public bool IsLocked =>
            User.LockoutEnd.HasValue &&
            User.LockoutEnd > DateTimeOffset.Now;
    }
}