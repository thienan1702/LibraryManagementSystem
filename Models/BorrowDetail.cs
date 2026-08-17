namespace LibraryManagement.Models
{
    public class BorrowDetail
    {
        public int Id { get; set; }

        public int BorrowId { get; set; }
        public Borrow? Borrow { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }

        // =========================
        // BORROW QUANTITY
        // =========================

        // Tổng số sách mượn
        public int Quantity { get; set; }


        // =========================
        // RETURN QUANTITY
        // =========================

        // Số lượng trả tốt
        public int GoodQuantity { get; set; }

        // Số lượng hư nhẹ
        public int MinorDamageQuantity { get; set; }

        // Số lượng hư nặng
        public int MajorDamageQuantity { get; set; }

        // Số lượng bị mất
        public int LostQuantity { get; set; }


        // =========================
        // DAMAGE INFORMATION
        // =========================

        // Mô tả tình trạng / hư hỏng / mất sách
        public string? DamageDescription { get; set; }

        // Tiền phạt do hư hỏng / mất sách
        public decimal DamageFee { get; set; }


        // =========================
        // RETURN CONDITION
        // =========================

        // Tình trạng tổng quát của lần trả
        public BookReturnCondition ReturnCondition { get; set; }
            = BookReturnCondition.Good;


        // =========================
        // CONDITION NOTE
        // =========================

        public string? ConditionNote { get; set; }


        // =========================
        // RETURN TIME
        // =========================

        public DateTime? ReturnedAt { get; set; }
    }
}