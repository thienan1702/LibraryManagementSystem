using LibraryManagement.Models;

namespace LibraryManagement.ViewModels
{
    public class BorrowReportVM
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string? Keyword { get; set; }

        public List<Borrow> Borrows { get; set; } = new();
    }
}