using LibraryManagement.Models;

namespace LibraryManagement.ViewModels;

public class FineReportVM
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? Keyword { get; set; }

    public List<Borrow> Borrows { get; set; } = new();

    public decimal TotalFine =>
        Borrows.Sum(x => x.FineAmount);

    public int TotalBorrow =>
        Borrows.Count;

    public int TotalReturned =>
        Borrows.Count(x => x.IsReturned);

    public int TotalLate =>
        Borrows.Count(x => x.FineAmount > 0);
}