using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class BorrowItemVM
    {
        [Required]
        public int BookId { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public string BookTitle { get; set; } = "";
    }
}