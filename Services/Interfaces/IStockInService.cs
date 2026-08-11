using LibraryManagement.Models;

namespace LibraryManagement.Services.Interfaces
{
    public interface IStockInService
    {
        Task<IEnumerable<StockIn>> GetAllAsync();

        Task<StockIn?> GetByIdAsync(int id);

        Task AddAsync(StockIn stockIn);
    }
}