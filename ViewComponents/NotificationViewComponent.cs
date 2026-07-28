using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var waiting = await _context.Reservations
                .Include(x => x.Book)
                .Where(x => x.Status == ReservationStatus.Waiting)
                .OrderByDescending(x => x.ReservationDate)
                .Take(5)
                .ToListAsync();

            ViewBag.Total = waiting.Count;

            return View(waiting);
        }
    }
}