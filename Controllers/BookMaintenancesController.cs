using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class BookMaintenancesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookMaintenancesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BookMaintenances
        public async Task<IActionResult> Index(
          string? search,
          MaintenanceStatus? status,
          int page = 1)
        {
            const int pageSize = 10;

            var query = _context.BookMaintenances
                .Include(x => x.Book)
                .AsQueryable();

            // =========================
            // SEARCH
            // =========================

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Book!.Title.Contains(search) ||
                    x.Reason.Contains(search));
            }

            // =========================
            // STATUS FILTER
            // =========================

            if (status.HasValue)
            {
                query = query.Where(x =>
                    x.Status == status.Value);
            }

            // =========================
            // STATISTICS
            // =========================
            // Tính trước khi phân trang

            ViewBag.TotalMaintenance =
                await query.CountAsync();

            ViewBag.Pending =
                await query.CountAsync(x =>
                    x.Status == MaintenanceStatus.Pending);

            ViewBag.InProgress =
                await query.CountAsync(x =>
                    x.Status == MaintenanceStatus.InProgress);

            ViewBag.Completed =
                await query.CountAsync(x =>
                    x.Status == MaintenanceStatus.Completed);

            // =========================
            // PAGINATION
            // =========================

            var totalItems = await query.CountAsync();

            var totalPages =
                (int)Math.Ceiling(
                    totalItems / (double)pageSize);

            // Không cho page vượt quá giới hạn
            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var maintenances = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // =========================
            // VIEWBAG
            // =========================

            ViewBag.Search = search;
            ViewBag.Status = status;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(maintenances);
        }

        // GET: BookMaintenances/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var maintenance = await _context.BookMaintenances
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (maintenance == null)
                return NotFound();

            return View(maintenance);
        }

        // POST: BookMaintenances/Start/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            var maintenance = await _context.BookMaintenances
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (maintenance == null)
                return NotFound();

            if (maintenance.Status != MaintenanceStatus.Pending)
            {
                TempData["Warning"] =
                    "This maintenance record cannot be started.";

                return RedirectToAction(nameof(Index));
            }

            maintenance.Status = MaintenanceStatus.InProgress;
            maintenance.StartedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Maintenance for '{maintenance.Book?.Title}' has started.";

            return RedirectToAction(nameof(Index));
        }

        // POST: BookMaintenances/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            int id,
            decimal cost)
        {
            var maintenance = await _context.BookMaintenances
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (maintenance == null)
                return NotFound();

            if (maintenance.Status != MaintenanceStatus.InProgress)
            {
                TempData["Warning"] =
                    "Only maintenance in progress can be completed.";

                return RedirectToAction(nameof(Index));
            }

            if (cost < 0)
            {
                TempData["Error"] =
                    "Maintenance cost cannot be negative.";

                return RedirectToAction(nameof(Index));
            }

            maintenance.Status = MaintenanceStatus.Completed;
            maintenance.CompletedAt = DateTime.Now;
            maintenance.Cost = cost;

            // =====================================
            // RETURN REPAIRED BOOKS TO INVENTORY
            // =====================================

            maintenance.Book!.AvailableQuantity +=
                maintenance.Quantity;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{maintenance.Quantity} repaired book(s) have been returned to inventory.";

            return RedirectToAction(nameof(Index));
        }

        // POST: BookMaintenances/Cancel/5
        // POST: BookMaintenances/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var maintenance = await _context.BookMaintenances
                .Include(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (maintenance == null)
                return NotFound();

            if (maintenance.Status == MaintenanceStatus.Completed)
            {
                TempData["Warning"] =
                    "Completed maintenance cannot be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (maintenance.Status == MaintenanceStatus.Cancelled)
            {
                TempData["Warning"] =
                    "This maintenance has already been cancelled.";

                return RedirectToAction(nameof(Index));
            }

            // ==============================
            // RETURN BOOKS TO INVENTORY
            // ==============================

            if (maintenance.Book != null)
            {
                maintenance.Book.AvailableQuantity +=
                    maintenance.Quantity;
            }

            maintenance.Status =
                MaintenanceStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{maintenance.Quantity} book(s) have been returned to inventory.";

            return RedirectToAction(nameof(Index));
        }
    }
}