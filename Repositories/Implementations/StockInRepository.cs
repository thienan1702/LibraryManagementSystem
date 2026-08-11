using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Repositories.Implementations
{
    public class StockInRepository : IStockInRepository
    {
        private readonly ApplicationDbContext _context;

        public StockInRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockIn>> GetAllAsync()
        {
            return await _context.StockIns
                .Include(x => x.Book)
                .OrderByDescending(x => x.StockInDate)
                .ToListAsync();
        }

        public async Task<StockIn?> GetByIdAsync(int id)
        {
            return await _context.StockIns
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(StockIn stockIn)
        {
            await _context.StockIns.AddAsync(stockIn);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}