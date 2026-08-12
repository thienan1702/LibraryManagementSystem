using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class AuthorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: Authors
        // =========================
        public IActionResult Index(string search, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var authors = _context.Authors.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                authors = authors.Where(x =>
                    x.Name.Contains(search) ||
                    x.Biography.Contains(search));
            }

            ViewBag.Search = search;

            return View(
                authors
                    .OrderBy(x => x.Name)
                    .ToPagedList(pageNumber, pageSize)
            );
        }

        // =========================
        // GET: Authors/Details/5
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (author == null)
                return NotFound();

            return View(author);
        }

        // =========================
        // GET: Authors/Create
        // =========================
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // POST: Authors/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Biography")] Author author)
        {
            if (!ModelState.IsValid)
                return View(author);

            _context.Add(author);

            await _context.SaveChangesAsync();

            // =========================
            // AUDIT LOG - CREATE
            // =========================
            await AddAuditLog(
                "Create",
                "Author",
                author.Id,
                $"Created author '{author.Name}'."
            );

            TempData["Success"] =
                "Author added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Authors/Edit/5
        // =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var author = await _context.Authors
                .FindAsync(id);

            if (author == null)
                return NotFound();

            return View(author);
        }

        // =========================
        // POST: Authors/Edit/5
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Biography")] Author author)
        {
            if (id != author.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(author);

            try
            {
                _context.Update(author);

                await _context.SaveChangesAsync();

                // =========================
                // AUDIT LOG - UPDATE
                // =========================
                await AddAuditLog(
                    "Update",
                    "Author",
                    author.Id,
                    $"Updated author '{author.Name}'."
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuthorExists(author.Id))
                    return NotFound();

                throw;
            }

            TempData["Success"] =
                "Author updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Authors/Delete/5
        // =========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (author == null)
                return NotFound();

            return View(author);
        }

        // =========================
        // POST: Authors/Delete/5
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _context.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null)
                return NotFound();

            if (author.Books.Any())
            {
                TempData["Error"] =
                    "Cannot delete this author because it is being used by one or more books.";

                return RedirectToAction(nameof(Index));
            }

            // Lưu thông tin trước khi xóa
            var authorName = author.Name;
            var authorId = author.Id;

            _context.Authors.Remove(author);

            await _context.SaveChangesAsync();

            // =========================
            // AUDIT LOG - DELETE
            // =========================
            await AddAuditLog(
                "Delete",
                "Author",
                authorId,
                $"Deleted author '{authorName}'."
            );

            TempData["Success"] =
                "Author deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // AUDIT LOG HELPER
        // =========================
        private async Task AddAuditLog(
            string action,
            string entity,
            int entityId,
            string description)
        {
            var userName =
                User?.Identity?.Name ?? "System";

            var auditLog = new AuditLog
            {
                UserName = userName,
                Action = action,
                Entity = entity,
                EntityId = entityId,
                Time = DateTime.Now,
                Description = description
            };

            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();
        }

        // =========================
        // AUTHOR EXISTS
        // =========================
        private bool AuthorExists(int id)
        {
            return _context.Authors
                .Any(e => e.Id == id);
        }
    }
}