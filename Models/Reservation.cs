using System.ComponentModel.DataAnnotations;

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

        [Required]
        public int BookId { get; set; }

        public Book? Book { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public DateTime ReservationDate { get; set; } = DateTime.Now;

        public ReservationStatus Status { get; set; }
            = ReservationStatus.Waiting;

        public string? Note { get; set; }
    }
}