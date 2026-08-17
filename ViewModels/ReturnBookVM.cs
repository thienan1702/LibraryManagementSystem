using LibraryManagement.Models;

namespace LibraryManagement.ViewModels
{
    public class ReturnBookVM
    {
        public int BorrowId { get; set; }

        public string BorrowerName { get; set; }

        public List<ReturnBookItemVM> Items { get; set; }
            = new();
    }

    public class ReturnBookItemVM
    {
        public int BorrowDetailId { get; set; }

        public string BookTitle { get; set; }

        public int Quantity { get; set; }

        public BookReturnCondition ReturnCondition { get; set; }

        public string? DamageDescription { get; set; }

        public decimal DamageFee { get; set; }
    }
}