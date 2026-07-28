using LibraryManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class FinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Danh sách tiền phạt
        public async Task<IActionResult> Index()
        {
            var fines = await _context.Borrows
                .Where(x => x.FineAmount > 0)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            return View(fines);
        }

        //Thanh toán
        public async Task<IActionResult> Pay(int id)
        {
            var borrow = await _context.Borrows.FindAsync(id);

            if (borrow == null)
                return NotFound();

            borrow.IsPaid = true;
            borrow.PaidDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Fine paid successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}