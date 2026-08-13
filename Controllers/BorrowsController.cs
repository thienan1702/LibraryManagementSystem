using ClosedXML.Excel;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Interfaces;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;

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
     bool overdue = false,
     int? page = 1)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var borrows = _context.Borrows
                .Include(x => x.BorrowDetails)
                .AsQueryable();

            // =========================
            // SEARCH
            // =========================
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();

                borrows = borrows.Where(x =>
                    x.BorrowerName.Contains(search));
            }

            // =========================
            // OVERDUE
            // =========================
            if (overdue)
            {
                borrows = borrows.Where(x =>
                    !x.IsReturned &&
                    x.DueDate < DateTime.Today);
            }

            // =========================
            // STATUS
            // =========================
            if (status.HasValue)
            {
                borrows = borrows.Where(x =>
                    x.IsReturned == status.Value);
            }

            // =========================
            // SORT
            // =========================
            borrows = borrows
                .OrderByDescending(x => x.BorrowDate);

            // =========================
            // PAGINATION
            // =========================
            var pagedBorrows = borrows.ToPagedList(
                pageNumber,
                pageSize);

            // =========================
            // VIEWBAG
            // =========================
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Overdue = overdue;

            // =========================
            // STATISTICS
            // =========================
            ViewBag.TotalBorrow = await _context.Borrows.CountAsync();

            ViewBag.TotalBorrowing = await _context.Borrows
                .CountAsync(x => !x.IsReturned);

            ViewBag.TotalReturned = await _context.Borrows
                .CountAsync(x => x.IsReturned);

            return View(pagedBorrows);
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



        public async Task<IActionResult> FineManagement(
     string search,
     string status,
     int? page)
        {
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var query = _context.Borrows
                .Where(x => x.FineAmount > 0)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.BorrowerName.Contains(search) ||
                    x.BorrowerEmail.Contains(search));
            }

            // Filter status
            if (status == "Paid")
            {
                query = query.Where(x => x.IsPaid);
            }
            else if (status == "Unpaid")
            {
                query = query.Where(x => !x.IsPaid);
            }

            // Statistics
            ViewBag.TotalFines = await query.CountAsync();

            ViewBag.TotalFineAmount =
                await query.SumAsync(x => (decimal?)x.FineAmount) ?? 0;

            ViewBag.PaidFines =
                await query.CountAsync(x => x.IsPaid);

            ViewBag.UnpaidFines =
                await query.CountAsync(x => !x.IsPaid);

            // Lấy dữ liệu trước
            var fines = await query
                .OrderByDescending(x => x.ReturnDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            // Sau đó mới phân trang
            var pagedFines = fines.ToPagedList(pageNumber, pageSize);

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(pagedFines);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(
            int id,
            string paymentMethod)
        {
            var borrow = await _context.Borrows
                .FirstOrDefaultAsync(x => x.Id == id);

            if (borrow == null)
                return NotFound();

            if (borrow.IsPaid)
            {
                TempData["Warning"] = "This fine has already been paid.";
                return RedirectToAction(nameof(FineManagement));
            }

            if (borrow.FineAmount <= 0)
            {
                TempData["Warning"] = "This borrow has no fine.";
                return RedirectToAction(nameof(FineManagement));
            }

            var paymentCode =
                "PAY-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var invoiceNumber =
                "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            var payment = new FinePayment
            {
                PaymentCode = paymentCode,

                BorrowId = borrow.Id,

                CustomerName = borrow.BorrowerName,

                CustomerEmail = borrow.BorrowerEmail,

                Amount = borrow.FineAmount,

                PaymentMethod = paymentMethod,

                PaymentDate = DateTime.Now,

                PaidBy = User.Identity?.Name ?? "System",

                InvoiceNumber = invoiceNumber
            };

            borrow.IsPaid = true;

            borrow.PaidDate = DateTime.Now;

            _context.FinePayments.Add(payment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Payment successful. Invoice: {invoiceNumber}";

            return RedirectToAction(nameof(FineManagement));
        }




        public async Task<IActionResult> PaymentHistory(
         string? search,
         string? paymentMethod,
         int page = 1)
        {
            int pageSize = 10;

            IQueryable<FinePayment> query = _context.FinePayments
                .OrderByDescending(x => x.PaymentDate);

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.CustomerName.Contains(search) ||
                    x.CustomerEmail.Contains(search) ||
                    x.PaymentCode.Contains(search) ||
                    (x.InvoiceNumber != null &&
                     x.InvoiceNumber.Contains(search)));
            }

            // Filter payment method
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(x =>
                    x.PaymentMethod == paymentMethod);
            }

            var payments = query.ToPagedList(page, pageSize);
            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;

            ViewBag.TotalAmount = await query.SumAsync(x => (decimal?)x.Amount) ?? 0;

            ViewBag.TotalPayments = await query.CountAsync();

            return View(payments);
        }


        public async Task<IActionResult> Invoice(int id)
        {
            var payment = await _context.FinePayments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }



        public async Task<IActionResult> PaymentDetails(int id)
        {
            var payment = await _context.FinePayments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }




        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var payment = await _context.FinePayments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.DefaultTextStyle(x =>
                        x.FontSize(11));

                    // HEADER
                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Column(column =>
                                {
                                    column.Item()
                                        .Text("LIBRARY MANAGEMENT")
                                        .Bold()
                                        .FontSize(20);

                                    column.Item()
                                        .Text("Library Management System")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);

                                    column.Item()
                                        .Text("Ho Chi Minh City, Vietnam")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                            row.ConstantItem(180)
                                .AlignRight()
                                .Column(column =>
                                {
                                    column.Item()
                                        .Text("INVOICE")
                                        .Bold()
                                        .FontSize(26);

                                    column.Item()
                                        .Text($"Invoice No: {payment.InvoiceNumber}")
                                        .FontSize(10);

                                    column.Item()
                                        .Text(
                                            $"Date: {payment.PaymentDate:dd/MM/yyyy}")
                                        .FontSize(10);
                                });
                        });


                    page.Content()
                        .PaddingTop(30)
                        .Column(column =>
                        {
                            // CUSTOMER
                            column.Item()
                                .Text("BILL TO")
                                .Bold()
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item()
                                .PaddingTop(8)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(12)
                                .Column(customer =>
                                {
                                    customer.Item()
                                        .Text(payment.CustomerName)
                                        .Bold()
                                        .FontSize(14);

                                    customer.Item()
                                        .Text(payment.CustomerEmail)
                                        .FontColor(
                                            Colors.Grey.Darken1);
                                });


                            column.Item()
                                .PaddingTop(30);


                            // PAYMENT TABLE
                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Background(Colors.Grey.Darken3)
                                            .Padding(10)
                                            .Text("Description")
                                            .FontColor(Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(Colors.Grey.Darken3)
                                            .Padding(10)
                                            .Text("Payment Method")
                                            .FontColor(Colors.White)
                                            .Bold();

                                        header.Cell()
                                            .Background(Colors.Grey.Darken3)
                                            .Padding(10)
                                            .AlignRight()
                                            .Text("Amount")
                                            .FontColor(Colors.White)
                                            .Bold();
                                    });

                                    table.Cell()
                                        .Padding(12)
                                        .Text("Library overdue fine");

                                    table.Cell()
                                        .Padding(12)
                                        .Text(payment.PaymentMethod);

                                    table.Cell()
                                        .Padding(12)
                                        .AlignRight()
                                        .Text(
                                            $"{payment.Amount:N0} đ");
                                });


                            // TOTAL
                            column.Item()
                                .PaddingTop(25)
                                .AlignRight()
                                .Column(total =>
                                {
                                    total.Item()
                                        .Text(text =>
                                        {
                                            text.Span("TOTAL: ")
                                                .Bold()
                                                .FontSize(16);

                                            text.Span(
                                                $"{payment.Amount:N0} đ")
                                                .Bold()
                                                .FontSize(18)
                                                .FontColor(
                                                    Colors.Green.Darken2);
                                        });
                                });


                            // PAYMENT INFO
                            column.Item()
                                .PaddingTop(30)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("PAYMENT INFORMATION")
                                        .Bold()
                                        .FontSize(12)
                                        .FontColor(
                                            Colors.Grey.Darken1);

                                    info.Item()
                                        .PaddingTop(8)
                                        .Text(
                                            $"Status: PAID");

                                    info.Item()
                                        .Text(
                                            $"Payment Method: {payment.PaymentMethod}");

                                    info.Item()
                                        .Text(
                                            $"Payment Date: {payment.PaymentDate:dd/MM/yyyy HH:mm:ss}");

                                    info.Item()
                                        .Text(
                                            $"Paid By: {(string.IsNullOrWhiteSpace(payment.PaidBy)
                                                ? "System"
                                                : payment.PaidBy)}");
                                });
                        });


                    // FOOTER
                    page.Footer()
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Spacing(3);

                            column.Item()
                                .Text(text =>
                                {
                                    text.Span("Thank you for using our library service.")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                            column.Item()
                                .Text(text =>
                                {
                                    text.Span("Page ")
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken1);

                                    text.CurrentPageNumber()
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken1);

                                    text.Span(" / ")
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken1);

                                    text.TotalPages()
                                        .FontSize(8)
                                        .FontColor(Colors.Grey.Darken1);
                                });
                        });
                });
            });

            var pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Invoice-{payment.InvoiceNumber}.pdf");
        }




        // ==============================
        // EXPORT BORROWS TO EXCEL
        // ==============================
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var borrows = await _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Borrow Records");

            // ==============================
            // TITLE
            // ==============================

            worksheet.Cell(1, 1).Value = "LIBRARY MANAGEMENT";
            worksheet.Range(1, 1, 1, 10).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 18;
            worksheet.Cell(1, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Cell(2, 1).Value = "Borrow Records";
            worksheet.Range(2, 1, 2, 10).Merge();

            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 13;
            worksheet.Cell(2, 1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // ==============================
            // HEADER
            // ==============================

            string[] headers =
            {
        "#",
        "Borrower",
        "Email",
        "Borrow Date",
        "Due Date",
        "Return Date",
        "Status",
        "Book",
        "Quantity",
        "Fine Amount"
    };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(4, i + 1);

                cell.Value = headers[i];

                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#343a40");

                cell.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                cell.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                cell.Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;
            }

            // ==============================
            // DATA
            // ==============================

            int row = 5;
            int stt = 1;

            foreach (var borrow in borrows)
            {
                var details = borrow.BorrowDetails?.ToList();

                // Nếu không có BorrowDetail
                if (details == null || details.Count == 0)
                {
                    worksheet.Cell(row, 1).Value = stt;
                    worksheet.Cell(row, 2).Value = borrow.BorrowerName;
                    worksheet.Cell(row, 3).Value = borrow.BorrowerEmail;

                    worksheet.Cell(row, 4).Value =
                        borrow.BorrowDate.ToString("dd/MM/yyyy");

                    worksheet.Cell(row, 5).Value =
                        borrow.DueDate.ToString("dd/MM/yyyy");

                    worksheet.Cell(row, 6).Value =
                        borrow.ReturnDate?.ToString("dd/MM/yyyy") ?? "-";

                    worksheet.Cell(row, 7).Value =
                        borrow.IsReturned ? "Returned" : "Borrowing";

                    worksheet.Cell(row, 8).Value = "-";
                    worksheet.Cell(row, 9).Value = 0;

                    worksheet.Cell(row, 10).Value =
                        borrow.FineAmount;

                    row++;
                    stt++;

                    continue;
                }

                foreach (var detail in details)
                {
                    worksheet.Cell(row, 1).Value = stt;
                    worksheet.Cell(row, 2).Value = borrow.BorrowerName;
                    worksheet.Cell(row, 3).Value = borrow.BorrowerEmail;

                    worksheet.Cell(row, 4).Value =
                        borrow.BorrowDate.ToString("dd/MM/yyyy");

                    worksheet.Cell(row, 5).Value =
                        borrow.DueDate.ToString("dd/MM/yyyy");

                    worksheet.Cell(row, 6).Value =
                        borrow.ReturnDate?.ToString("dd/MM/yyyy") ?? "-";

                    worksheet.Cell(row, 7).Value =
                        borrow.IsReturned ? "Returned" : "Borrowing";

                    worksheet.Cell(row, 8).Value =
                        detail.Book?.Title ?? "Unknown Book";

                    worksheet.Cell(row, 9).Value =
                        detail.Quantity;

                    worksheet.Cell(row, 10).Value =
                        borrow.FineAmount;

                    row++;
                }

                stt++;
            }

            // ==============================
            // FORMAT
            // ==============================

            var dataRange = worksheet.Range(
                4,
                1,
                Math.Max(row - 1, 4),
                10);

            dataRange.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            dataRange.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            dataRange.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            // Center specific columns
            worksheet.Column(1).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(5).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(6).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(7).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            worksheet.Column(9).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            // Currency
            worksheet.Column(10)
                .Style.NumberFormat.Format = "#,##0";

            // ==============================
            // WIDTH
            // ==============================

            worksheet.Column(1).Width = 7;
            worksheet.Column(2).Width = 25;
            worksheet.Column(3).Width = 30;
            worksheet.Column(4).Width = 15;
            worksheet.Column(5).Width = 15;
            worksheet.Column(6).Width = 15;
            worksheet.Column(7).Width = 15;
            worksheet.Column(8).Width = 35;
            worksheet.Column(9).Width = 12;
            worksheet.Column(10).Width = 18;

            // ==============================
            // FREEZE HEADER
            // ==============================

            worksheet.SheetView.FreezeRows(4);

            // ==============================
            // AUTO FILTER
            // ==============================

            worksheet.Range(
                4,
                1,
                Math.Max(row - 1, 4),
                10
            ).SetAutoFilter();

            // ==============================
            // DOWNLOAD
            // ==============================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            string fileName =
                $"BorrowRecords_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
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
