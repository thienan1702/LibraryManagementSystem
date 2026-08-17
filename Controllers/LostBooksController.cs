using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class LostBooksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LostBooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LostBooks
        public async Task<IActionResult> Index(
            string? search,
            int page = 1)
        {
            const int pageSize = 10;

            var query = _context.LostBooks
                .Include(x => x.Book)
                .Include(x => x.Borrow)
                .AsQueryable();

            // =========================
            // SEARCH
            // =========================

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Book!.Title.Contains(search) ||
                    x.Borrow!.BorrowerName.Contains(search));
            }

            // =========================
            // STATISTICS
            // =========================

            ViewBag.TotalRecords =
                await query.CountAsync();

            ViewBag.TotalLostQuantity =
                await query.SumAsync(x => (int?)x.Quantity) ?? 0;

            ViewBag.TotalFine =
                await query.SumAsync(x => (decimal?)x.FineAmount) ?? 0;

            // =========================
            // PAGINATION
            // =========================

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            if (page < 1)
                page = 1;

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var lostBooks = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // =========================
            // VIEWBAG
            // =========================

            ViewBag.Search = search;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(lostBooks);
        }


        // GET: LostBooks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var lostBook = await _context.LostBooks
                .Include(x => x.Book)
                .Include(x => x.Borrow)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (lostBook == null)
                return NotFound();

            return View(lostBook);
        }
    }
}