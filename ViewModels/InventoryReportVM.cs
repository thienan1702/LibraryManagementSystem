using LibraryManagement.Models;

namespace LibraryManagement.ViewModels;

public class InventoryReportVM
{
    public string? Keyword { get; set; }

    public List<Book> Books { get; set; } = new();

    public int TotalBooks { get; set; }

    public int TotalTitles { get; set; }

    public int AvailableBooks { get; set; }

    public int BorrowedBooks { get; set; }

    public int LowStock { get; set; }

    public int OutOfStock { get; set; }
}