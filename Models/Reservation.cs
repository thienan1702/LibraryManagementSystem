using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public int BookId { get; set; }

        public Book Book { get; set; }

        public DateTime ReserveDate { get; set; } = DateTime.Now;

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    }
}