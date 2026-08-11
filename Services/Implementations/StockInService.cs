using LibraryManagement.Models;
using LibraryManagement.Repositories.Interfaces;
using LibraryManagement.Services.Interfaces;

namespace LibraryManagement.Services.Implementations
{
    public class StockInService : IStockInService
    {
        private readonly IStockInRepository _repository;

        public StockInService(IStockInRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<StockIn>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<StockIn?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(StockIn stockIn)
        {
            await _repository.AddAsync(stockIn);
            await _repository.SaveAsync();
        }
    }
}