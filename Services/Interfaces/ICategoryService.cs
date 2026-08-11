using LibraryManagement.Models;
using X.PagedList;

namespace LibraryManagement.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IPagedList<Category>> GetPagedAsync(
            string? search,
            string? sortOrder,
            int page,
            int pageSize);

        Task<Category?> GetByIdAsync(int id);

        Task AddAsync(Category category);

        Task UpdateAsync(Category category);

        Task<bool> DeleteAsync(int id);
    }
}