using LibraryManagement.Models;
using X.PagedList;

namespace LibraryManagement.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();

        Task<IEnumerable<Category>> SearchAsync(string keyword);

        Task<IPagedList<Category>> GetPagedAsync(
            string? search,
            string? sortOrder,
            int page,
            int pageSize);

        Task<Category?> GetByIdAsync(int id);

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);

        Task DeleteAsync(int id);

        Task SaveAsync();

        Task<bool> HasBooksAsync(int id);
    }
}