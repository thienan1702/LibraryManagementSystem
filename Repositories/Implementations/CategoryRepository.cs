using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace LibraryManagement.Repositories.Implementations
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<Category>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await _dbSet.ToListAsync();

            return await _dbSet
                .Where(x => x.Name.Contains(keyword))
                .ToListAsync();
        }
        public async Task<IPagedList<Category>> GetPagedAsync(
            string? search,
            string? sortOrder,
            int page,
            int pageSize)
        {
            IQueryable<Category> query = _context.Categories;

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Name.Contains(search));
            }

            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(x => x.Name),
                _ => query.OrderBy(x => x.Name)
            };

            return query.ToPagedList(page, pageSize);
        }
    }
}