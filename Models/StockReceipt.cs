using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    [Index(nameof(ReceiptCode), IsUnique = true)]
    public class StockReceipt
    {
        public int Id { get; set; }


        // =========================
        // RECEIPT INFORMATION
        // =========================

        [Required]
        [StringLength(50)]
        public string ReceiptCode { get; set; }
            = string.Empty;

        [Required]
        public DateTime ReceiptDate { get; set; }
            = DateTime.Now;


        // =========================
        // SUPPLIER
        // =========================

        [Required]
        public int SupplierId { get; set; }

        public Supplier? Supplier { get; set; }


        // =========================
        // CREATED BY
        // =========================

        [StringLength(100)]
        public string? CreatedBy { get; set; }


        // =========================
        // NOTE
        // =========================

        [StringLength(1000)]
        public string? Note { get; set; }


        // =========================
        // TOTAL
        // =========================

        public decimal TotalAmount { get; set; }


        // =========================
        // DETAILS
        // =========================

        public ICollection<StockReceiptDetail> Details { get; set; }
            = new List<StockReceiptDetail>();
    }
}