using LibraryManagement.Models;

namespace LibraryManagement.Repositories.Interfaces
{
    public interface IStockInRepository
    {
        Task<IEnumerable<StockIn>> GetAllAsync();

        Task<StockIn?> GetByIdAsync(int id);

        Task AddAsync(StockIn stockIn);

        Task SaveAsync();
    }
}