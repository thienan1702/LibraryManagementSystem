using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        // =========================
        // SUPPLIER INFORMATION
        // =========================

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }


        // =========================
        // STATUS
        // =========================

        public bool IsActive { get; set; } = true;


        // =========================
        // STOCK RECEIPTS
        // =========================

        public ICollection<StockReceipt> StockReceipts { get; set; }
            = new List<StockReceipt>();
    }
}