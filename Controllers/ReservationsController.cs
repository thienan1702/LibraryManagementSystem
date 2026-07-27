using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReservationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.User)
                .OrderByDescending(r => r.ReserveDate)
                .ToListAsync();

            return View(reservations);
        }

        public async Task<IActionResult> Create(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
                return NotFound();

            if (book.AvailableQuantity > 0)
            {
                TempData["Warning"] =
                    "This book is available. Please borrow it directly.";

                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            string userId = _userManager.GetUserId(User)!;

            bool existed = await _context.Reservations.AnyAsync(x =>
                    x.BookId == bookId &&
                    x.UserId == userId &&
                    x.Status == ReservationStatus.Pending);

            if (existed)
            {
                TempData["Warning"] =
                    "You have already reserved this book.";

                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            Reservation reservation = new Reservation
            {
                BookId = bookId,
                UserId = userId,
                ReserveDate = DateTime.Now,
                Status = ReservationStatus.Pending
            };

            _context.Reservations.Add(reservation);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation created successfully.";

            return RedirectToAction("Details", "Books", new { id = bookId });
        }
    }
}