using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryManagement.ViewModels;
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
        private readonly BorrowReceiptService _receiptService;


        public BorrowsController(ApplicationDbContext context,IEmailService email,IPdfService pdf, IConfiguration configuration, BorrowReceiptService receiptService)

        {
            _context = context;
            _email = email;
            _pdf = pdf;
            _configuration = configuration;
            _receiptService = receiptService;


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

        public IActionResult Create()
        {
            ViewBag.Books = _context.Books
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Include(x => x.Publisher)
                .Where(x => x.AvailableQuantity > 0)
                .OrderBy(x => x.Title)
                .ToList();

            return View(new BorrowCreateVM());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BorrowCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(vm);
            }

            //=========================================================
            // BUSINESS RULE
            //=========================================================

            // 1. Người này còn sách chưa trả?
            bool hasOverdue = await _context.Borrows.AnyAsync(x =>
               x.BorrowerEmail == vm.BorrowerEmail &&
               !x.IsReturned &&
               x.DueDate < DateTime.Today);

            // 2. Người này còn tiền phạt chưa thanh toán?
            bool hasUnpaidFine = await _context.Borrows.AnyAsync(x =>
                x.BorrowerEmail == vm.BorrowerEmail &&
                x.FineAmount > 0 &&
                !x.IsPaid);

            // 3. Đang giữ bao nhiêu quyển sách?
            int currentBooks = await _context.BorrowDetails
                .Include(x => x.Borrow)
                .Where(x =>
                    x.Borrow.BorrowerEmail == vm.BorrowerEmail &&
                    !x.Borrow.IsReturned)
                .SumAsync(x => x.Quantity);

            // 4. Người dùng đang mượn thêm bao nhiêu quyển?
            int requestBooks = vm.Items?.Sum(x => x.Quantity) ?? 0;

            if (hasOverdue)
            {
                ModelState.AddModelError("",
                    "Borrower has overdue books.");
            }

            if (hasUnpaidFine)
            {
                ModelState.AddModelError("",
                    "This borrower still has unpaid fines.");
            }

            if (currentBooks + requestBooks > 3)
            {
                ModelState.AddModelError("",
                    "A borrower can borrow a maximum of 3 books.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(vm);
            }

            //=========================================================
            // Kiểm tra dữ liệu mượn
            //=========================================================

            if (vm.Items == null || vm.Items.Count == 0)
            {
                ModelState.AddModelError("", "Please choose at least one book.");

                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(vm);
            }

            foreach (var item in vm.Items)
            {
                var book = await _context.Books.FindAsync(item.BookId);

                if (book == null)
                {
                    ModelState.AddModelError("", "Book not found.");
                }
                else if (book.AvailableQuantity < item.Quantity)
                {
                    ModelState.AddModelError("", $"{book.Title} does not have enough quantity.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Books = _context.Books
                    .Where(x => x.AvailableQuantity > 0)
                    .OrderBy(x => x.Title)
                    .ToList();

                return View(vm);
            }

            //=========================================================
            // Tạo Borrow
            //=========================================================

            Borrow borrow = new Borrow
            {
                BorrowerName = vm.BorrowerName,
                BorrowerEmail = vm.BorrowerEmail,
                BorrowDate = vm.BorrowDate,
                DueDate = vm.DueDate,
                IsReturned = false,
                ReturnDate = null,
                FineAmount = 0,
                IsPaid = true
            };

            _context.Borrows.Add(borrow);

            await _context.SaveChangesAsync();
            //=========================================================
            // Tạo BorrowDetail + cập nhật số lượng sách
            //=========================================================

            foreach (var item in vm.Items)
            {
                var book = await _context.Books.FindAsync(item.BookId);

                BorrowDetail detail = new BorrowDetail
                {
                    BorrowId = borrow.Id,
                    BookId = item.BookId,
                    Quantity = item.Quantity
                };

                _context.BorrowDetails.Add(detail);

                // Giảm số lượng sách còn lại
                book.AvailableQuantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();


            //================ EMAIL ================

            string rows = "";

            foreach (var item in vm.Items)
            {
                var book = await _context.Books.FindAsync(item.BookId);

                rows += $@"
<tr>
    <td>{book.Title}</td>
    <td>{item.Quantity}</td>
</tr>";
            }

            await _email.SendAsync(
                vm.BorrowerEmail,
                "Library Borrow Confirmation",
                $@"
<h2>Library Management</h2>

<p>Hello <b>{vm.BorrowerName}</b></p>

<p>Your borrowing request has been created successfully.</p>

<table border='1' cellpadding='8' cellspacing='0' width='100%'>

<tr style='background:#0d6efd;color:white'>

<th>Book</th>

<th>Quantity</th>

</tr>

{rows}

</table>

<br>

<p><b>Borrow Date:</b> {vm.BorrowDate:dd/MM/yyyy}</p>

<p><b>Due Date:</b> {vm.DueDate:dd/MM/yyyy}</p>

<p>Thank you for using our library.</p>");

            TempData["Success"] = "Borrow created successfully.";

            return RedirectToAction(
                nameof(BorrowReceipt),
                new { id = borrow.Id });
        }


        // GET: Borrows/Create
        //public IActionResult Create(string? userId = null, int? bookId = null)
        //{
        //    ViewBag.Users = _context.Users
        //        .OrderBy(x => x.FullName)
        //        .ToList();

        //    ViewBag.Books = _context.Books
        //        .Where(x => x.AvailableQuantity > 0)
        //        .OrderBy(x => x.Title)
        //        .ToList();

        //    ViewBag.SelectedUser = userId;

        //    ViewBag.SelectedBook = bookId;

        //    return View();
        //}
        //// POST: Borrows/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(Borrow borrow,int BookId,int Quantity)
        //{

        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Users = _context.Users
        //            .OrderBy(x => x.FullName)
        //            .ToList();

        //        ViewBag.Books = _context.Books
        //            .Where(x => x.AvailableQuantity > 0)
        //            .OrderBy(x => x.Title)
        //            .ToList();

        //        return View(borrow);
        //    }

        //    var book = await _context.Books.FindAsync(BookId);
        //    if (Quantity <= 0)
        //    {
        //        ModelState.AddModelError("", "Quantity must be greater than zero.");
        //    }

        //    if (borrow.DueDate <= borrow.BorrowDate)
        //    {
        //        ModelState.AddModelError("", "Due Date must be after Borrow Date.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        ViewBag.Users = _context.Users.OrderBy(x => x.FullName).ToList();

        //        ViewBag.Books = _context.Books
        //            .Where(x => x.AvailableQuantity > 0)
        //            .OrderBy(x => x.Title)
        //            .ToList();

        //        return View(borrow);
        //    }

        //    if (book == null)
        //        return NotFound();

        //    if (book.AvailableQuantity < Quantity)
        //    {
        //        ModelState.AddModelError("", "Not enough books.");

        //        ViewBag.Users = _context.Users
        //            .OrderBy(x => x.FullName)
        //            .ToList();

        //        ViewBag.Books = _context.Books
        //            .Where(x => x.AvailableQuantity > 0)
        //            .OrderBy(x => x.Title)
        //            .ToList();

        //        return View(borrow);
        //    }


        //    borrow.ReturnDate = null;

        //    borrow.IsReturned = false;

        //    _context.Borrows.Add(borrow);

        //    await _context.SaveChangesAsync();

        //    BorrowDetail detail = new BorrowDetail()
        //    {
        //        BorrowId = borrow.Id,
        //        BookId = BookId,
        //        Quantity = Quantity
        //    };

        //    _context.BorrowDetails.Add(detail);

        //    book.AvailableQuantity -= Quantity;

        //    await _context.SaveChangesAsync();

        //    // ===== GỬI EMAIL =====
        //    await _email.SendAsync(
        //        borrow.BorrowerEmail, 
        //        "Library Borrow Confirmation",
        //        $@"
        //<h2>Library Management</h2>

        //<p>Hello <b>{borrow.BorrowerName}</b>,</p>

        //<p>Your borrowing request has been created successfully.</p>

        //<table border='1' cellpadding='8' cellspacing='0'>
        //    <tr>
        //        <td>Book</td>
        //        <td>{book.Title}</td>
        //    </tr>

        //    <tr>
        //        <td>Quantity</td>
        //        <td>{Quantity}</td>
        //    </tr>

        //    <p><b>Borrow Date:</b> 
        //        {borrow.BorrowDate:dd/MM/yyyy}</p>

        //    <p><b>Due Date:</b> 
        //        {borrow.DueDate:dd/MM/yyyy}</p>
        //</table>

        //<br/>

        //<p>Thank you for using our library.</p>");

        //    TempData["Success"] = "Borrow created successfully.";

        //    return RedirectToAction(nameof(Index));
        //}

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

            // Chưa trả sách → không được xóa
            if (!borrow.IsReturned)
            {
                TempData["Error"] =
                    "Cannot delete this borrow because the books have not been returned.";

                return RedirectToAction(nameof(Index));
            }

            // Chưa thanh toán fine → không được xóa
            if (!borrow.IsPaid)
            {
                TempData["Error"] =
                    "Cannot delete this borrow because the fine has not been paid.";

                return RedirectToAction(nameof(Index));
            }

            // Xóa BorrowDetail
            _context.BorrowDetails.RemoveRange(borrow.BorrowDetails);

            // Xóa Borrow
            _context.Borrows.Remove(borrow);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Borrow deleted successfully.";

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

            // Đánh dấu đã trả
            borrow.ReturnDate = DateTime.Now;
            borrow.IsReturned = true;

            //==============================
            // Fine Management
            //==============================

            const decimal finePerDay = 10000;

            if (borrow.ReturnDate.Value.Date > borrow.DueDate.Date)
            {
                int overdueDays =
                    (borrow.ReturnDate.Value.Date - borrow.DueDate.Date).Days;

                borrow.FineAmount = overdueDays * finePerDay;

                // Có tiền phạt => chưa thanh toán
                borrow.IsPaid = false;
            }
            else
            {
                borrow.FineAmount = 0;

                // Không có tiền phạt => xem như đã thanh toán
                borrow.IsPaid = true;
            }

            //==============================
            // Cập nhật lại số lượng sách
            //==============================

            foreach (var detail in borrow.BorrowDetails)
            {
                detail.Book.AvailableQuantity += detail.Quantity;
            }

            await _context.SaveChangesAsync();

            //==============================
            // Xử lý Reservation
            //==============================

            foreach (var detail in borrow.BorrowDetails)
            {
                await ProcessReservation(detail.BookId);
            }

            if (borrow.FineAmount > 0)
            {
                TempData["Warning"] =
                    $"Book returned successfully. Fine: {borrow.FineAmount:N0} VND. Please complete payment.";
            }
            else
            {
                TempData["Success"] = "Book returned successfully.";
            }

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



        public async Task<IActionResult> FineManagement()
        {
            var fines = await _context.Borrows
                .Where(x => x.FineAmount > 0)
                .OrderByDescending(x => x.ReturnDate)
                .ToListAsync();

            return View(fines);
        }

        [HttpPost]
        public async Task<IActionResult> PayFine(int id)
        {
            var borrow = await _context.Borrows.FindAsync(id);

            if (borrow == null)
                return NotFound();

            borrow.IsPaid = true;
            borrow.PaidDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Fine paid successfully.";

            return RedirectToAction(nameof(FineManagement));
        }

        public IActionResult Print(int id)
        {
            var pdf = _pdf.GenerateBorrowPdf(id);

            return File(
                pdf,
                "application/pdf",
                $"Borrow_{id}.pdf");
        }


        public async Task<IActionResult> PrintReceipt(int id)
        {
            var borrow = await _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            var pdf = _receiptService.Generate(borrow);

            return File(
                pdf,
                "application/pdf",
                $"Borrow_{borrow.Id}.pdf");
        }


        public IActionResult BorrowReceipt(int id)
        {
            var borrow = _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                        .ThenInclude(x => x.Author)
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                        .ThenInclude(x => x.Category)
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                        .ThenInclude(x => x.Publisher)
                .FirstOrDefault(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            return View(borrow);
        }


        private bool BorrowExists(int id)
        {
            return _context.Borrows.Any(e => e.Id == id);
        }
    }
}
