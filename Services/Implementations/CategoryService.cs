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

        // =========================
        // GET PAGED
        // =========================
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

        // =========================
        // GET BY ID
        // =========================
        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // =========================
        // CREATE
        // =========================
        public async Task AddAsync(Category category)
        {
            await _repository.AddAsync(category);
        }

        // =========================
        // UPDATE
        // =========================
        public async Task UpdateAsync(Category category)
        {
            var existing = await _repository.GetByIdAsync(category.Id);

            if (existing == null)
                return;

            existing.Name = category.Name;
            existing.Description = category.Description;

            // existing đang được EF Core tracking
            // nên chỉ cần SaveChanges
            await _repository.SaveAsync();
        }

        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);

            if (category == null)
                return false;

            var hasBooks = await _repository.HasBooksAsync(id);

            if (hasBooks)
                return false;

            await _repository.DeleteAsync(id);

            await _repository.SaveAsync();

            return true;
        }
    }
}