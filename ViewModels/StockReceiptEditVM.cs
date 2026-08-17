using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.ViewModels
{
    public class StockReceiptEditVM
    {
        public int Id { get; set; }

        public string ReceiptCode { get; set; } = string.Empty;

        [Required]
        public DateTime ReceiptDate { get; set; }

        [Required]
        public int SupplierId { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string? Note { get; set; }

        public List<StockReceiptEditDetailVM> Details { get; set; }
            = new List<StockReceiptEditDetailVM>();
    }


    public class StockReceiptEditDetailVM
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string? Note { get; set; }
    }
}