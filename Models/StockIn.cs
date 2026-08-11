using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class StockIn
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a book.")]
        public int BookId { get; set; }

        public Book? Book { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Stock-in date is required.")]
        public DateTime StockInDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }
}