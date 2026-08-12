using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]

    public class StockInsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockInService _service;
        private readonly IAuditService _audit;

        public StockInsController(
            ApplicationDbContext context,
            IStockInService service,
            IAuditService audit)
        {
            _context = context;
            _service = service;
            _audit = audit;
        }

        // GET: StockIns
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var stockIns = await _context.StockIns
                .Include(x => x.Book)
                .OrderByDescending(x => x.StockInDate)
                .ToListAsync();

            // Statistics - tính trên toàn bộ dữ liệu
            ViewBag.TotalEntries = stockIns.Count;

            ViewBag.TotalBooks = stockIns.Sum(x => x.Quantity);

            ViewBag.TotalBooksRestocked = stockIns
                .Select(x => x.BookId)
                .Distinct()
                .Count();

            // Pagination
            var pagedStockIns = stockIns.ToPagedList(
                pageNumber,
                pageSize
            );

            return View(pagedStockIns);
        }

        // GET: StockIns/Create
        public async Task<IActionResult> Create()
        {
            ViewData["BookId"] = new SelectList(
                await _context.Books
                    .OrderBy(x => x.Title)
                    .ToListAsync(),
                "Id",
                "Title");

            return View(new StockIn
            {
                StockInDate = DateTime.Now
            });
        }

        // POST: StockIns/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockIn stockIn)
        {
            if (stockIn.Quantity <= 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Quantity must be greater than 0.");
            }

            var book = await _context.Books
                .FirstOrDefaultAsync(x => x.Id == stockIn.BookId);

            if (book == null)
            {
                ModelState.AddModelError(
                    "BookId",
                    "Book not found.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["BookId"] = new SelectList(
                    await _context.Books
                        .OrderBy(x => x.Title)
                        .ToListAsync(),
                    "Id",
                    "Title",
                    stockIn.BookId);

                return View(stockIn);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Tăng tổng số lượng sách
                book!.Quantity += stockIn.Quantity;

                // Tăng số lượng sách có thể mượn
                book.AvailableQuantity += stockIn.Quantity;

                stockIn.StockInDate =
                    stockIn.StockInDate == default
                        ? DateTime.Now
                        : stockIn.StockInDate;

                stockIn.CreatedBy =
                    User.Identity?.Name ?? "System";

                await _service.AddAsync(stockIn);

                await _audit.SaveAsync(
                    User.Identity?.Name ?? "System",
                    "Stock In",
                    "Book",
                    book.Id,
                    $"Stocked in {stockIn.Quantity} copy/copies of '{book.Title}'");

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Stocked in {stockIn.Quantity} copy/copies successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "An error occurred while stocking in the book.";

                return RedirectToAction(nameof(Create));
            }
        }
    }
}