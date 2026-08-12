using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class PublishersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublishersController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET: Publishers
        // =========================
        public async Task<IActionResult> Index(
            string? search,
            int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var query = _context.Publishers
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Name.Contains(search) ||
                    x.Address.Contains(search));
            }

            query = query.OrderBy(x => x.Name);

            var publishers = query.ToPagedList(
                pageNumber,
                pageSize);

            ViewBag.Search = search;

            return View(publishers);
        }

        // =========================
        // GET: Publishers/Details/5
        // =========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var publisher = await _context.Publishers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (publisher == null)
                return NotFound();

            return View(publisher);
        }

        // =========================
        // GET: Publishers/Create
        // =========================
        public IActionResult Create()
        {
            return View();
        }

        // =========================
        // POST: Publishers/Create
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Address")] Publisher publisher)
        {
            if (!ModelState.IsValid)
                return View(publisher);

            _context.Add(publisher);

            await _context.SaveChangesAsync();

            // =========================
            // AUDIT LOG - CREATE
            // =========================
            await AddAuditLog(
                "Create",
                "Publisher",
                publisher.Id,
                $"Created publisher '{publisher.Name}'."
            );

            TempData["Success"] =
                "Publisher added successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Publishers/Edit/5
        // =========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var publisher = await _context.Publishers
                .FindAsync(id);

            if (publisher == null)
                return NotFound();

            return View(publisher);
        }

        // =========================
        // POST: Publishers/Edit/5
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Address")] Publisher publisher)
        {
            if (id != publisher.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(publisher);

            try
            {
                _context.Update(publisher);

                await _context.SaveChangesAsync();

                // =========================
                // AUDIT LOG - UPDATE
                // =========================
                await AddAuditLog(
                    "Update",
                    "Publisher",
                    publisher.Id,
                    $"Updated publisher '{publisher.Name}'."
                );
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PublisherExists(publisher.Id))
                    return NotFound();

                throw;
            }

            TempData["Success"] =
                "Publisher updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // GET: Publishers/Delete/5
        // =========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var publisher = await _context.Publishers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (publisher == null)
                return NotFound();

            return View(publisher);
        }

        // =========================
        // POST: Publishers/Delete/5
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var publisher = await _context.Publishers
                .FindAsync(id);

            if (publisher == null)
                return NotFound();

            var hasBooks = await _context.Books
                .AnyAsync(x =>
                    x.PublisherId == id);

            if (hasBooks)
            {
                TempData["Error"] =
                    "Cannot delete this publisher because it is being used by one or more books.";

                return RedirectToAction(nameof(Index));
            }

            // Lưu thông tin trước khi xóa
            var publisherName = publisher.Name;
            var publisherId = publisher.Id;

            _context.Publishers.Remove(publisher);

            await _context.SaveChangesAsync();

            // =========================
            // AUDIT LOG - DELETE
            // =========================
            await AddAuditLog(
                "Delete",
                "Publisher",
                publisherId,
                $"Deleted publisher '{publisherName}'."
            );

            TempData["Success"] =
                "Publisher deleted successfully.";

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
        // PUBLISHER EXISTS
        // =========================
        private bool PublisherExists(int id)
        {
            return _context.Publishers
                .Any(e => e.Id == id);
        }
    }
}