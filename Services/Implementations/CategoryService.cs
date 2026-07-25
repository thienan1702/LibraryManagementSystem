using LibraryManagement.Models;
using LibraryManagement.Repositories.Interfaces;
using LibraryManagement.Services.Interfaces;
using X.PagedList;

namespace LibraryManagement.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IPagedList<Category>> GetPagedAsync(
            string? search,
            string? sortOrder,
            int page,
            int pageSize)
        {
            return await _repository.GetPagedAsync(
                search,
                sortOrder,
                page,
                pageSize);
        }
        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task AddAsync(Category category)
        {
            await _repository.AddAsync(category);
            await _repository.SaveAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            await _repository.UpdateAsync(category);
            await _repository.SaveAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
            await _repository.SaveAsync();
        }

        public async Task<IEnumerable<Category>> SearchAsync(string keyword)
        {
            return await _repository.SearchAsync(keyword);
        }
    }
}