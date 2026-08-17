using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class StockReceiptDetail
    {
        public int Id { get; set; }


        // =========================
        // RECEIPT
        // =========================

        [Required]
        public int StockReceiptId { get; set; }

        public StockReceipt? StockReceipt { get; set; }


        // =========================
        // BOOK
        // =========================

        [Required]
        public int BookId { get; set; }

        public Book? Book { get; set; }


        // =========================
        // QUANTITY
        // =========================

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Quantity must be greater than 0."
        )]
        public int Quantity { get; set; }


        // =========================
        // UNIT PRICE
        // =========================

        [Range(
            0,
            double.MaxValue,
            ErrorMessage = "Unit price cannot be negative."
        )]
        public decimal UnitPrice { get; set; }


        // =========================
        // AMOUNT
        // =========================

        public decimal Amount
        {
            get
            {
                return Quantity * UnitPrice;
            }
        }


        // =========================
        // NOTE
        // =========================

        [StringLength(500)]
        public string? Note { get; set; }
    }
}