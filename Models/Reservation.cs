using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = "";

        public DateTime ReservationDate { get; set; } = DateTime.Now;

        public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

        public int BookId { get; set; }

        [ForeignKey(nameof(BookId))]
        public Book? Book { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; } = 1;
    }
}