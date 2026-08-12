using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class FinePayment
    {
        public int Id { get; set; }

        // Mã giao dịch
        [Required]
        public string PaymentCode { get; set; } = "";

        // Borrow liên quan
        public int BorrowId { get; set; }

        public Borrow? Borrow { get; set; }

        // Người thanh toán
        [Required]
        public string CustomerName { get; set; } = "";

        public string CustomerEmail { get; set; } = "";

        // Số tiền
        [Required]
        public decimal Amount { get; set; }

        // Cash / Bank Transfer
        [Required]
        public string PaymentMethod { get; set; } = "";

        // Thời gian thanh toán
        public DateTime PaymentDate { get; set; }

        // Người thực hiện giao dịch
        public string PaidBy { get; set; } = "";

        // Mã hóa đơn
        public string? InvoiceNumber { get; set; }
    }
}