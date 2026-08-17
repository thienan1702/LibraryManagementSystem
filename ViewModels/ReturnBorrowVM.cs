using LibraryManagement.Models;

namespace LibraryManagement.ViewModels
{
    public class ReturnBorrowVM
    {
        public int BorrowId { get; set; }

        public string BorrowerName { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; }

        public DateTime DueDate { get; set; }

        public List<ReturnBorrowDetailVM> Details { get; set; }
            = new List<ReturnBorrowDetailVM>();
    }

    public class ReturnBorrowDetailVM
    {
        public int BorrowDetailId { get; set; }

        public int BookId { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        // Tổng số lượng mượn
        public int Quantity { get; set; }

        // Số lượng trả tốt
        public int GoodQuantity { get; set; }

        // Số lượng hư nhẹ
        public int MinorDamageQuantity { get; set; }

        // Số lượng hư nặng
        public int MajorDamageQuantity { get; set; }

        // Số lượng mất
        public int LostQuantity { get; set; }

        // Mô tả hư hỏng
        public string? DamageDescription { get; set; }

        // Ghi chú
        public string? ConditionNote { get; set; }
    }
}