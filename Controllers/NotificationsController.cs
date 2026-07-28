using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetReservationNotification()
        {
            var data = await _context.Reservations
                .Include(x => x.Book)
                .Where(x => x.Status == ReservationStatus.Waiting)
                .OrderByDescending(x => x.ReservationDate)
                .Take(5)
                .Select(x => new
                {
                    x.Id,
                    x.CustomerName,
                    Book = x.Book.Title,
                    Date = x.ReservationDate.ToString("dd/MM/yyyy")
                })
                .ToListAsync();

            return Json(new
            {
                total = data.Count,
                items = data
            });
        }
    }
}