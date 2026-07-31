namespace LibraryManagement.Models.ViewModels
{
    public class TopBookViewModel
    {
        public string BookTitle { get; set; } = "";

        public string Author { get; set; } = "";

        public string Category { get; set; } = "";

        public int BorrowCount { get; set; }
    }
}