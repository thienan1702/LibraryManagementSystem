using LibraryManagement.Models;
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

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        // GET
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

        // GET
        public async Task<IActionResult> Details(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // GET
        public IActionResult Create()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            await _service.AddAsync(category);
            TempData["Success"] = "Category added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(category);

            await _service.UpdateAsync(category);
            TempData["Success"] = "Category updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _service.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
            {
                TempData["Error"] =
                    "Cannot delete this category because it is being used by one or more books.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}