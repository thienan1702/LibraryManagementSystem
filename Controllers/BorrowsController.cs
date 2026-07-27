using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class BorrowsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly IPdfService _pdf;
        private readonly IConfiguration _configuration;

        public BorrowsController(ApplicationDbContext context,IEmailService email,IPdfService pdf, IConfiguration configuration)

        {
            _context = context;
            _email = email;
            _pdf = pdf;
            _configuration = configuration;

        }



        // GET: Borrows
        public async Task<IActionResult> Index(
           string search,
            bool? status,
            bool overdue = false)
        {
            var borrows = _context.Borrows
                .Include(x => x.BorrowDetails)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                borrows = borrows.Where(x =>
                    x.BorrowerName.Contains(search));
            }
            if (overdue)
            {
                borrows = borrows.Where(x =>
                 !x.IsReturned &&
                 x.DueDate < DateTime.Today);
            }
            if (status.HasValue)
            {
                borrows = borrows.Where(x =>
                    x.IsReturned == status);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Overdue = overdue;

            return View(await borrows.ToListAsync());
        }


        [HttpGet]
        public IActionResult GetBorrowerInfo(string email)
        {
            var borrows = _context.Borrows
                .Where(x => x.BorrowerEmail == email);

            return Json(new
            {
                exists = borrows.Any(),
                totalBorrow = borrows.Count(),
                borrowing = borrows.Count(x => !x.IsReturned)
            });
        }

        // GET: Borrows/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var borrow = await _context.Borrows
                .Include(x => x.BorrowDetails)
                .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            return View(borrow);
        }

        // GET: Borrows/Create
        public IActionResult Create(string? userId = null, int? bookId = null)
        {
            ViewBag.Users = _context.Users
                .OrderBy(x => x.FullName)
                .ToList();

            ViewBag.Books = _context.Books
                .Where(x => x.AvailableQuantity > 0)
                .OrderBy(x => x.Title)
                .ToList();

            ViewBag.SelectedUser = userId;

            ViewBag.SelectedBook = bookId;

            return View();
        }

        // POST: Borrows/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Borrow borrow,int BookId,int Quantity)
        {
            
            if (!ModelState.IsValid)
            {
                ViewBag.Users = _context.Users
                    .OrderBy(x => x.FullName)
                    .ToList();

                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(borrow);
            }

            var book = await _context.Books.FindAsync(BookId);
            if (Quantity <= 0)
            {
                ModelState.AddModelError("", "Quantity must be greater than zero.");
            }

            if (borrow.DueDate <= borrow.BorrowDate)
            {
                ModelState.AddModelError("", "Due Date must be after Borrow Date.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = _context.Users.OrderBy(x => x.FullName).ToList();

                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(borrow);
            }

            if (book == null)
                return NotFound();

            if (book.AvailableQuantity < Quantity)
            {
                ModelState.AddModelError("", "Not enough books.");

                ViewBag.Users = _context.Users
                    .OrderBy(x => x.FullName)
                    .ToList();

                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(borrow);
            }


            borrow.ReturnDate = null;

            borrow.IsReturned = false;

            _context.Borrows.Add(borrow);

            await _context.SaveChangesAsync();

            BorrowDetail detail = new BorrowDetail()
            {
                BorrowId = borrow.Id,
                BookId = BookId,
                Quantity = Quantity
            };

            _context.BorrowDetails.Add(detail);

            book.AvailableQuantity -= Quantity;

            await _context.SaveChangesAsync();

            // ===== GỬI EMAIL =====
            await _email.SendAsync(
                borrow.BorrowerEmail, 
                "Library Borrow Confirmation",
                $@"
        <h2>Library Management</h2>

        <p>Hello <b>{borrow.BorrowerName}</b>,</p>

        <p>Your borrowing request has been created successfully.</p>

        <table border='1' cellpadding='8' cellspacing='0'>
            <tr>
                <td>Book</td>
                <td>{book.Title}</td>
            </tr>

            <tr>
                <td>Quantity</td>
                <td>{Quantity}</td>
            </tr>

            <p><b>Borrow Date:</b> 
                {borrow.BorrowDate:dd/MM/yyyy}</p>

            <p><b>Due Date:</b> 
                {borrow.DueDate:dd/MM/yyyy}</p>
        </table>

        <br/>

        <p>Thank you for using our library.</p>");

            TempData["Success"] = "Borrow created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Borrows/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrow = await _context.Borrows.FindAsync(id);
            if (borrow == null)
            {
                return NotFound();
            }
            return View(borrow);
        }

        // POST: Borrows/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BorrowerName,BorrowDate,DueDate,ReturnDate,IsReturned,BorrowerEmail")] Borrow borrow)
        {
            if (id != borrow.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(borrow);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BorrowExists(borrow.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(borrow);
        }

        // GET: Borrows/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var borrow = await _context.Borrows
                .FirstOrDefaultAsync(m => m.Id == id);
            if (borrow == null)
            {
                return NotFound();
            }

            return View(borrow);
        }

        // POST: Borrows/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrow = await _context.Borrows
                .Include(x => x.BorrowDetails)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            // Nếu chưa trả sách thì cộng lại số lượng
            if (!borrow.IsReturned)
            {
                foreach (var item in borrow.BorrowDetails)
                {
                    var book = await _context.Books.FindAsync(item.BookId);

                    if (book != null)
                    {
                        book.AvailableQuantity += item.Quantity;
                    }
                }
            }

            _context.BorrowDetails.RemoveRange(borrow.BorrowDetails);

            _context.Borrows.Remove(borrow);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Borrow deleted.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Return(int id)
        {
            var borrow = await _context.Borrows
                .Include(x => x.BorrowDetails)
                .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            borrow.ReturnDate = DateTime.Now;
            borrow.IsReturned = true;

            const decimal finePerDay = 10000;

            if (borrow.ReturnDate.Value.Date > borrow.DueDate.Date)
            {
                int overdue =
                    (borrow.ReturnDate.Value.Date - borrow.DueDate.Date).Days;

                borrow.FineAmount = overdue * finePerDay;
            }
            else
            {
                borrow.FineAmount = 0;
            }

            foreach (var detail in borrow.BorrowDetails)
            {
                detail.Book.AvailableQuantity += detail.Quantity;
            }

            await _context.SaveChangesAsync();

            foreach (var detail in borrow.BorrowDetails)
            {
                await ProcessReservation(detail.BookId);
            }

            TempData["Success"] = "Book returned successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ProcessReservation(int bookId)
        {
            var reservation = await _context.Reservations
                .Include(x => x.Book)
                .Where(x =>
                    x.BookId == bookId &&
                    x.Status == ReservationStatus.Waiting)
                .OrderBy(x => x.ReservationDate)
                .FirstOrDefaultAsync();

            if (reservation == null)
                return;

            reservation.Status = ReservationStatus.Approved;

            await _context.SaveChangesAsync();

            await _email.SendAsync(
                reservation.CustomerEmail,
                "Reservation Approved",
        $@"
<h2>Library Management</h2>

<p>Hello <b>{reservation.CustomerName}</b>,</p>

<p>Your reservation has been approved.</p>

<p>The book below is now available:</p>

<table border='1' cellpadding='8' cellspacing='0'>
<tr>
<td><b>Book</b></td>
<td>{reservation.Book.Title}</td>
</tr>
</table>

<br/>

<p>Please come to the library to borrow your book.</p>

<p>Thank you.</p>");
        }

        public IActionResult Print(int id)
        {
            var pdf = _pdf.GenerateBorrowPdf(id);

            return File(
                pdf,
                "application/pdf",
                $"Borrow_{id}.pdf");
        }

        private bool BorrowExists(int id)
        {
            return _context.Borrows.Any(e => e.Id == id);
        }
    }
}
