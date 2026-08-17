using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class LostBook
    {
        public int Id { get; set; }

        // =========================
        // BOOK
        // =========================

        [Required]
        public int BookId { get; set; }

        public Book? Book { get; set; }


        // =========================
        // BORROW
        // =========================

        [Required]
        public int BorrowId { get; set; }

        public Borrow? Borrow { get; set; }


        // =========================
        // QUANTITY
        // =========================

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }


        // =========================
        // FINE
        // =========================

        [Range(0, double.MaxValue)]
        public decimal FineAmount { get; set; }


        // =========================
        // INFORMATION
        // =========================

        public string? Note { get; set; }


        // =========================
        // TIME
        // =========================

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;
    }
}