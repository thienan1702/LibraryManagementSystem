using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class BorrowCreateVM
    {
        [Required]
        public string BorrowerName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string BorrowerEmail { get; set; } = "";

        [Required]
        public DateTime BorrowDate { get; set; }
            = DateTime.Today;

        [Required]
        public DateTime DueDate { get; set; }
            = DateTime.Today.AddDays(14);

        public List<BorrowItemVM> Items { get; set; }
            = new();
    }
}