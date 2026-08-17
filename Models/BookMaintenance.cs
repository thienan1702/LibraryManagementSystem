using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class BookMaintenance
    {
        public int Id { get; set; }

        // =========================
        // BOOK
        // =========================

        [Required]
        public int BookId { get; set; }

        public Book? Book { get; set; }


        // =========================
        // QUANTITY
        // =========================

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }


        // =========================
        // MAINTENANCE INFORMATION
        // =========================

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        public string? Note { get; set; }


        // =========================
        // STATUS
        // =========================

        public MaintenanceStatus Status { get; set; }
            = MaintenanceStatus.Pending;


        // =========================
        // COST
        // =========================

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; }


        // =========================
        // TIME
        // =========================

        public DateTime CreatedAt { get; set; }
            = DateTime.Now;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }


    public enum MaintenanceStatus
    {
        Pending = 0,

        InProgress = 1,

        Completed = 2,

        Cancelled = 3
    }
}