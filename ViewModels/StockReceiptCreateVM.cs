using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class StockReceiptCreateVM
    {
        [Required]
        [StringLength(50)]
        public string ReceiptCode { get; set; }
            = string.Empty;

        [Required]
        public DateTime ReceiptDate { get; set; }
            = DateTime.Now;

        [Required]
        public int SupplierId { get; set; }

        public string? CreatedBy { get; set; }

        [StringLength(1000)]
        public string? Note { get; set; }

        public List<StockReceiptDetailCreateVM> Details { get; set; }
            = new List<StockReceiptDetailCreateVM>();
    }


    public class StockReceiptDetailCreateVM
    {
        [Required]
        public int BookId { get; set; }

        public string BookTitle { get; set; }
            = string.Empty;

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Quantity must be greater than 0."
        )]
        public int Quantity { get; set; }

        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Unit price cannot be negative."
        )]
        public decimal UnitPrice { get; set; }

        public decimal Amount =>
            Quantity * UnitPrice;
    }
}