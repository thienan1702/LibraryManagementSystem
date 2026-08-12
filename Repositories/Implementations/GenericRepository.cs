using LibraryManagement.Data;
using LibraryManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Repositories.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task UpdateAsync(T entity)
        {
            var key = _context.Model
                .FindEntityType(typeof(T))
                ?.FindPrimaryKey()
                ?.Properties
                .FirstOrDefault();

            if (key == null)
                throw new InvalidOperationException(
                    $"Entity {typeof(T).Name} does not have a primary key.");

            var keyValue = key.PropertyInfo?.GetValue(entity);

            var existingEntity = await _dbSet.FindAsync(keyValue);

            if (existingEntity == null)
                return;

            _context.Entry(existingEntity)
                .CurrentValues
                .SetValues(entity);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}