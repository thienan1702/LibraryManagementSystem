namespace LibraryManagement.Models
{
    public class BorrowDetail
    {
        public int Id { get; set; }

        public int BorrowId { get; set; }

        public Borrow? Borrow { get; set; }

        public int BookId { get; set; }

        public Book? Book { get; set; }

        public int Quantity { get; set; }
    }
}