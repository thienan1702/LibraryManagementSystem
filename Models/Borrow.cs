using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    public class Borrow
    {
        public int Id { get; set; }

        [Required]
        public string BorrowerName { get; set; } = "";

        [EmailAddress]
        public string BorrowerEmail { get; set; } = "";

        [Required]
        public DateTime BorrowDate { get; set; }

        // Hạn phải trả
        [Required]
        public DateTime DueDate { get; set; }

        // Ngày trả thực tế
        public DateTime? ReturnDate { get; set; }

        public bool IsReturned { get; set; }

        [NotMapped]
        public bool IsOverdue =>
            !IsReturned &&
            DateTime.Today > DueDate;

        [NotMapped]
        public int OverdueDays =>
            IsOverdue
                ? (DateTime.Today - DueDate).Days
                : 0;

        public decimal FineAmount { get; set; }

        public ICollection<BorrowDetail> BorrowDetails { get; set; }
            = new List<BorrowDetail>();
    }
}