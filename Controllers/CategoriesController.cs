using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _service;
        private readonly ApplicationDbContext _context;

        public CategoriesController(
            ICategoryService service,
            ApplicationDbContext context)
        {
            _service = service;
            _context = context;
        }

        // =========================
        // GET: Categories
        // =========================
        public async Task<IActionResult> Index(
            string? search,
            string? sortOrder,
            int page = 1)
        {
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;

            ViewBag.NameSort =
                sortOrder == "name_desc"
                ? ""
                : "name_desc";

            var model = await _service.GetPagedAsync(
                search,
                sortOrder,
                page,
                5);

            return View(model);
        }

        // =========================
        // GET: Categories/Details/5
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================
        // GET: Categories/Create
        // =========================
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // POST: Categories/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            await _service.AddAsync(category);

            // =========================
            // AUDIT LOG - CREATE
            // =========================
            var userName = User.Identity?.Name ?? "System";

            var auditLog = new AuditLog
            {
                UserName = userName,
                Action = "Create",
                Entity = "Category",
                EntityId = category.Id,
                Time = DateTime.Now,
                Description =
                    $"Created category '{category.Name}'."
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Categories/Edit/5
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================
        // POST: Categories/Edit/5
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
       int id,
       Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(category);

            // Lấy dữ liệu cũ bằng AsNoTracking
            var oldCategory = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (oldCategory == null)
                return NotFound();

            var oldName = oldCategory.Name;

            // Update
            await _service.UpdateAsync(category);

            // Audit
            var userName = User.Identity?.Name ?? "System";

            var auditLog = new AuditLog
            {
                UserName = userName,
                Action = "Update",
                Entity = "Category",
                EntityId = category.Id,
                Time = DateTime.Now,
                Description =
                    $"Updated category from '{oldName}' to '{category.Name}'."
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Categories/Delete/5
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // =========================
        // POST: Categories/Delete/5
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Lấy thông tin category trước khi xóa
            var category =
                await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            var categoryName = category.Name;

            var deleted =
                await _service.DeleteAsync(id);

            if (!deleted)
            {
                TempData["Error"] =
                    "Cannot delete this category because it is being used by one or more books.";

                return RedirectToAction(nameof(Index));
            }

            // =========================
            // AUDIT LOG - DELETE
            // =========================
            var userName = User.Identity?.Name ?? "System";

            var auditLog = new AuditLog
            {
                UserName = userName,
                Action = "Delete",
                Entity = "Category",
                EntityId = id,
                Time = DateTime.Now,
                Description =
                    $"Deleted category '{categoryName}'."
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}   