using LibraryManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            ViewBag.TotalBooks = await _context.Books.CountAsync();
            ViewBag.TotalAuthors = await _context.Authors.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            ViewBag.TotalPublishers = await _context.Publishers.CountAsync();

            ViewBag.TotalBorrow = await _context.Borrows.CountAsync();
            ViewBag.Borrowing = await _context.Borrows.CountAsync(x => !x.IsReturned);
            ViewBag.Returned = await _context.Borrows.CountAsync(x => x.IsReturned);

            ViewBag.TopBooks = await _context.BorrowDetails
                .Include(x => x.Book)
                .GroupBy(x => x.Book!.Title)
                .Select(g => new
                {
                    Name = g.Key,
                    Total = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();
            ViewBag.LowStockBooks = _context.Books
                .Where(x => x.AvailableQuantity <= 5)
                .OrderBy(x => x.AvailableQuantity)
                .Take(5)
                .ToList();

            ViewBag.NewBooks = _context.Books
                .OrderByDescending(x => x.Id)
                .Take(5)
                .ToList();

            var monthlyBorrow = new int[12];

            for (int i = 1; i <= 12; i++)
            {
                monthlyBorrow[i - 1] = _context.Borrows
                    .Count(x => x.BorrowDate.Month == i);
            }

            ViewBag.MonthlyBorrow =
                JsonSerializer.Serialize(monthlyBorrow);

            ViewBag.Overdue =
                _context.Borrows
                .Where(x =>
                    !x.IsReturned &&
                    x.ReturnDate < DateTime.Now)
                .ToList();

            return View();
        }

    }
}