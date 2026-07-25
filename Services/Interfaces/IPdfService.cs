namespace LibraryManagement.Services.Interfaces
{
    public interface IPdfService
    {
        byte[] GenerateBorrowPdf(int borrowId);
    }
}