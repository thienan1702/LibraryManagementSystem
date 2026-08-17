using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StockReceiptsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockReceiptsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        // ==============================
        // INDEX
        // ==============================

        public async Task<IActionResult> Index(
      string? search,
      int? page)
        {   
            int pageNumber = page ?? 1;
            int pageSize = 10;

            var query = _context.StockReceipts
                .Include(x => x.Supplier)
                .Include(x => x.Details)
                .AsQueryable();

            // ==============================
            // SEARCH
            // ==============================

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.ReceiptCode.Contains(search) ||
                    (x.Supplier != null &&
                     x.Supplier.Name.Contains(search)));
            }

            // ==============================
            // ORDER
            // ==============================

            query = query
                .OrderByDescending(x => x.ReceiptDate);

            // ==============================
            // GET DATA
            // ==============================

            var receiptList =
                await query.ToListAsync();

            // ==============================
            // PAGINATION
            // ==============================

            var receipts =
                receiptList.ToPagedList(
                    pageNumber,
                    pageSize);

            // ==============================
            // STATISTICS
            // ==============================

            ViewBag.TotalReceipts =
                await _context.StockReceipts
                    .CountAsync();

            ViewBag.TotalBooks =
                await _context.StockReceiptDetails
                    .SumAsync(x => x.Quantity);

            ViewBag.TotalImportValue =
                await _context.StockReceipts
                    .SumAsync(x => x.TotalAmount);

            ViewBag.Search = search;

            return View(receipts);
        }
        // ==============================
        // DETAILS
        // ==============================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var receipt = await _context.StockReceipts
                .Include(x => x.Supplier)
                .Include(x => x.Details)
                    .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();

            return View(receipt);
        }


        // ==============================
        // CREATE - GET
        // ==============================

        // ==============================
        // CREATE - GET
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCreateData();

            var model = new StockReceiptCreateVM
            {
                ReceiptCode = await GenerateReceiptCode(),
                ReceiptDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "System"
            };

            return View(model);
        }


        // ==============================
        // CREATE - POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            StockReceiptCreateVM model)
        {
            // ==============================
            // VALIDATE SUPPLIER
            // ==============================

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SupplierId &&
                    x.IsActive);

            if (supplier == null)
            {
                ModelState.AddModelError(
                    "SupplierId",
                    "Please select a valid active supplier.");
            }


            // ==============================
            // VALIDATE DETAILS
            // ==============================

            if (model.Details == null ||
                model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one book.");
            }


            if (model.Details != null)
            {
                foreach (var detail in model.Details)
                {
                    if (detail.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Book quantity must be greater than 0.");
                    }

                    if (detail.UnitPrice < 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unit price cannot be negative.");
                    }
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadCreateData();

                return View(model);
            }


            // ==============================
            // CHECK DUPLICATE BOOK
            // ==============================

            var duplicateBookIds =
                model.Details
                    .GroupBy(x => x.BookId)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();

            if (duplicateBookIds.Any())
            {
                ModelState.AddModelError(
                    "",
                    "The same book cannot appear multiple times in one receipt.");

                await LoadCreateData();

                return View(model);
            }


            // ==============================
            // CREATE RECEIPT
            // ==============================

            var receipt = new StockReceipt
            {
                ReceiptCode = await GenerateReceiptCode(),
                ReceiptDate = model.ReceiptDate,
                SupplierId = model.SupplierId,
                CreatedBy =
                    User.Identity?.Name
                    ?? model.CreatedBy
                    ?? "System",
                Note = model.Note
            };


            decimal totalAmount = 0;


            // ==============================
            // PROCESS DETAILS
            // ==============================

            foreach (var item in model.Details)
            {
                var book = await _context.Books
                    .FirstOrDefaultAsync(x =>
                        x.Id == item.BookId);

                if (book == null)
                {
                    ModelState.AddModelError(
                        "",
                        "One of the selected books does not exist.");

                    await LoadCreateData();

                    return View(model);
                }


                decimal amount =
                    item.Quantity * item.UnitPrice;


                var detail =
                    new StockReceiptDetail
                    {
                        BookId = book.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Note = null
                    };


                receipt.Details.Add(detail);


                totalAmount += amount;


                // ==============================
                // UPDATE STOCK
                // ==============================

                book.Quantity += item.Quantity;

                book.AvailableQuantity +=
                    item.Quantity;
            }


            // ==============================
            // TOTAL
            // ==============================

            receipt.TotalAmount =
                totalAmount;


            // ==============================
            // SAVE
            // ==============================

            _context.StockReceipts.Add(receipt);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Stock receipt '{receipt.ReceiptCode}' " +
                "has been created successfully.";


            return RedirectToAction(
                nameof(Details),
                new { id = receipt.Id });
        }

        // ==============================
        // GENERATE RECEIPT CODE
        // ==============================

        private async Task<string> GenerateReceiptCode()
        {
            string date = DateTime.Now.ToString("yyyyMMdd");

            string prefix = $"PN-{date}-";

            var lastReceipt = await _context.StockReceipts
                .Where(x => x.ReceiptCode.StartsWith(prefix))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastReceipt != null)
            {
                string numberPart =
                    lastReceipt.ReceiptCode
                        .Substring(prefix.Length);

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:000}";
        }


        // GET: StockReceipts/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var receipt = await _context.StockReceipts
                .Include(x => x.Supplier)
                .Include(x => x.Details)
                    .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();

            return View(receipt);
        }


        // POST: StockReceipts/Delete/5
        // ==============================
        // DELETE - POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // ==============================
            // LOAD RECEIPT
            // ==============================

            var receipt = await _context.StockReceipts
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();


            // ==============================
            // CHECK STOCK
            // ==============================

            foreach (var detail in receipt.Details)
            {
                var book = await _context.Books
                    .FirstOrDefaultAsync(x =>
                        x.Id == detail.BookId);

                if (book == null)
                {
                    TempData["Error"] =
                        $"Book with ID {detail.BookId} no longer exists.";

                    return RedirectToAction(nameof(Index));
                }


                // ==========================================
                // KHÔNG ĐƯỢC XÓA NẾU AVAILABLE KHÔNG ĐỦ
                // ==========================================

                if (book.AvailableQuantity < detail.Quantity)
                {
                    TempData["Error"] =
                        $"Cannot delete receipt '{receipt.ReceiptCode}'. " +
                        $"Book '{book.Title}' has only " +
                        $"{book.AvailableQuantity} available book(s), " +
                        $"but this receipt contains {detail.Quantity}. " +
                        $"Some books may currently be borrowed, lost, " +
                        $"or under maintenance.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id = receipt.Id });
                }


                // ==========================================
                // QUANTITY CANNOT BECOME NEGATIVE
                // ==========================================

                if (book.Quantity < detail.Quantity)
                {
                    TempData["Error"] =
                        $"Cannot delete receipt because the stock quantity " +
                        $"of '{book.Title}' would become negative.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id = receipt.Id });
                }
            }


            // ==============================
            // UPDATE BOOK STOCK
            // ==============================

            foreach (var detail in receipt.Details)
            {
                var book = await _context.Books
                    .FirstAsync(x =>
                        x.Id == detail.BookId);


                book.Quantity -=
                    detail.Quantity;

                book.AvailableQuantity -=
                    detail.Quantity;
            }


            // ==============================
            // DELETE RECEIPT
            // ==============================

            _context.StockReceipts.Remove(receipt);


            // ==============================
            // SAVE
            // ==============================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Stock receipt '{receipt.ReceiptCode}' " +
                "has been deleted successfully.";


            return RedirectToAction(nameof(Index));
        }
        // ==============================
        // EDIT - GET
        // ==============================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var receipt = await _context.StockReceipts
                .Include(x => x.Details)
                    .ThenInclude(x => x.Book)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();

            var model = new StockReceiptEditVM
            {
                Id = receipt.Id,

                ReceiptCode = receipt.ReceiptCode,

                ReceiptDate = receipt.ReceiptDate,

                SupplierId = receipt.SupplierId,

                CreatedBy = receipt.CreatedBy,

                Note = receipt.Note,

                Details = receipt.Details
                    .Select(x => new StockReceiptEditDetailVM
                    {
                        Id = x.Id,

                        BookId = x.BookId,

                        BookTitle = x.Book?.Title ?? "Unknown Book",

                        Quantity = x.Quantity,

                        UnitPrice = x.UnitPrice,

                        Note = x.Note
                    })
                    .ToList()
            };

            await LoadEditData();

            return View(model);
        }

        // ==============================
        // EDIT - POST
        // ==============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            StockReceiptEditVM model)
        {
            if (id != model.Id)
                return NotFound();

            // ==============================
            // LOAD RECEIPT
            // ==============================

            var receipt = await _context.StockReceipts
                .Include(x => x.Details)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();


            // ==============================
            // VALIDATE SUPPLIER
            // ==============================

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(x =>
                    x.Id == model.SupplierId &&
                    x.IsActive);

            if (supplier == null)
            {
                ModelState.AddModelError(
                    nameof(model.SupplierId),
                    "Please select a valid active supplier.");
            }


            // ==============================
            // VALIDATE DETAILS
            // ==============================

            if (model.Details == null ||
                model.Details.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please add at least one book.");
            }


            if (model.Details != null)
            {
                foreach (var detail in model.Details)
                {
                    if (detail.Quantity <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Book quantity must be greater than 0.");
                    }

                    if (detail.UnitPrice < 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Unit price cannot be negative.");
                    }
                }


                // ==============================
                // CHECK DUPLICATE BOOK
                // ==============================

                var duplicateBookIds =
                    model.Details
                        .GroupBy(x => x.BookId)
                        .Where(x => x.Count() > 1)
                        .Select(x => x.Key)
                        .ToList();

                if (duplicateBookIds.Any())
                {
                    ModelState.AddModelError(
                        "",
                        "The same book cannot appear multiple times in one receipt.");
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadEditData();

                return View(model);
            }


            // ==============================
            // LOAD BOOKS
            // ==============================

            var bookIds = model.Details
                .Select(x => x.BookId)
                .Distinct()
                .ToList();

            var books = await _context.Books
                .Where(x => bookIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);


            if (books.Count != bookIds.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected books do not exist.");

                await LoadEditData();

                return View(model);
            }


            // ==================================================
            // BUILD OLD QUANTITY BY BOOK
            // ==================================================

            var oldQuantities = receipt.Details
                .GroupBy(x => x.BookId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(d => d.Quantity)
                );


            // ==================================================
            // BUILD NEW QUANTITY BY BOOK
            // ==================================================

            var newQuantities = model.Details
                .GroupBy(x => x.BookId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(d => d.Quantity)
                );


            // ==================================================
            // CHECK STOCK BEFORE REDUCING
            // ==================================================

            foreach (var oldItem in oldQuantities)
            {
                int bookId = oldItem.Key;

                int oldQuantity = oldItem.Value;

                int newQuantity =
                    newQuantities.ContainsKey(bookId)
                        ? newQuantities[bookId]
                        : 0;

                // Số lượng bị giảm khỏi phiếu
                int decrease =
                    oldQuantity - newQuantity;

                if (decrease <= 0)
                    continue;


                var book = await _context.Books
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookId);

                if (book == null)
                {
                    ModelState.AddModelError(
                        "",
                        "A book from the original receipt no longer exists.");

                    await LoadEditData();

                    return View(model);
                }


                // ==================================================
                // QUAN TRỌNG
                //
                // Không được giảm AvailableQuantity nhiều hơn
                // số sách hiện đang Available.
                // ==================================================

                if (book.AvailableQuantity < decrease)
                {
                    ModelState.AddModelError(
                        "",
                        $"Cannot reduce '{book.Title}' by {decrease} book(s). " +
                        $"Only {book.AvailableQuantity} book(s) are currently available."
                    );
                }


                // Tổng Quantity cũng không được âm
                if (book.Quantity < decrease)
                {
                    ModelState.AddModelError(
                        "",
                        $"Cannot reduce stock of '{book.Title}' " +
                        "because the quantity would become negative."
                    );
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadEditData();

                return View(model);
            }


            // ==================================================
            // APPLY STOCK DIFFERENCE
            // ==================================================

            var allBookIds =
                oldQuantities.Keys
                    .Union(newQuantities.Keys)
                    .Distinct()
                    .ToList();


            foreach (var bookId in allBookIds)
            {
                var book = await _context.Books
                    .FirstOrDefaultAsync(x =>
                        x.Id == bookId);

                if (book == null)
                    continue;


                int oldQuantity =
                    oldQuantities.ContainsKey(bookId)
                        ? oldQuantities[bookId]
                        : 0;


                int newQuantity =
                    newQuantities.ContainsKey(bookId)
                        ? newQuantities[bookId]
                        : 0;


                int difference =
                    newQuantity - oldQuantity;


                // ==========================================
                // TĂNG SỐ LƯỢNG NHẬP
                // ==========================================

                if (difference > 0)
                {
                    book.Quantity += difference;

                    book.AvailableQuantity += difference;
                }


                // ==========================================
                // GIẢM SỐ LƯỢNG NHẬP
                // ==========================================

                else if (difference < 0)
                {
                    int decrease =
                        Math.Abs(difference);

                    book.Quantity -= decrease;

                    book.AvailableQuantity -= decrease;
                }
            }


            // ==================================================
            // UPDATE RECEIPT
            // ==================================================

            receipt.ReceiptDate =
                model.ReceiptDate;

            receipt.SupplierId =
                model.SupplierId;

            receipt.Note =
                model.Note;


            // ==================================================
            // REMOVE OLD DETAILS
            // ==================================================

            _context.StockReceiptDetails.RemoveRange(
                receipt.Details);


            // ==================================================
            // ADD NEW DETAILS
            // ==================================================

            decimal totalAmount = 0;


            foreach (var item in model.Details)
            {
                var detail = new StockReceiptDetail
                {
                    StockReceiptId = receipt.Id,

                    BookId = item.BookId,

                    Quantity = item.Quantity,

                    UnitPrice = item.UnitPrice,

                    Note = item.Note
                };


                receipt.Details.Add(detail);


                totalAmount +=
                    item.Quantity * item.UnitPrice;
            }


            // ==================================================
            // UPDATE TOTAL
            // ==================================================

            receipt.TotalAmount =
                totalAmount;


            // ==================================================
            // SAVE
            // ==================================================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                $"Stock receipt '{receipt.ReceiptCode}' " +
                "has been updated successfully.";


            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = receipt.Id
                });
        }



        // ==============================
        // EXPORT STOCK RECEIPT - PDF
        // ==============================

        public async Task<IActionResult> ExportInvoice(int id)
        {
            var receipt = await _context.StockReceipts
                .Include(x => x.Supplier)
                .Include(x => x.Details)
                    .ThenInclude(x => x.Book)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (receipt == null)
                return NotFound();

            QuestPDF.Settings.License =
                LicenseType.Community;


            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    page.MarginHorizontal(40);
                    page.MarginVertical(35);

                    page.DefaultTextStyle(
                        TextStyle.Default
                            .FontFamily("Arial")
                            .FontSize(9));


                    // =====================================================
                    // HEADER
                    // =====================================================

                    page.Header()
                        .Column(header =>
                        {
                            header.Item()
                                .Row(row =>
                                {
                                    // LOGO / SYSTEM NAME
                                    row.RelativeItem(2)
                                        .Column(left =>
                                        {
                                            left.Item()
                                                .Text("LIBRARY")
                                                .Bold()
                                                .FontSize(20)
                                                .FontColor("#0D6EFD");

                                            left.Item()
                                                .Text("MANAGEMENT SYSTEM")
                                                .Bold()
                                                .FontSize(9)
                                                .FontColor("#6C757D");
                                        });


                                    // DOCUMENT TITLE
                                    row.RelativeItem(3)
                                        .AlignRight()
                                        .Column(right =>
                                        {
                                            right.Item()
                                                .Text("PHIẾU NHẬP KHO")
                                                .Bold()
                                                .FontSize(18)
                                                .FontColor("#212529");

                                            right.Item()
                                                .PaddingTop(3)
                                                .Text("STOCK RECEIPT")
                                                .FontSize(9)
                                                .FontColor("#6C757D");
                                        });
                                });


                            header.Item()
                                .PaddingTop(12)
                                .LineHorizontal(2)
                                .LineColor("#0D6EFD");
                        });


                    // =====================================================
                    // CONTENT
                    // =====================================================

                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {

                            // =================================================
                            // RECEIPT INFORMATION
                            // =================================================

                            column.Item()
                                .Background("#F8F9FA")
                                .Border(1)
                                .BorderColor("#DEE2E6")
                                .Padding(12)
                                .Row(row =>
                                {

                                    // LEFT
                                    row.RelativeItem()
                                        .Column(left =>
                                        {
                                            left.Item()
                                                .Text("THÔNG TIN PHIẾU NHẬP")
                                                .Bold()
                                                .FontSize(10)
                                                .FontColor("#0D6EFD");

                                            left.Item()
                                                .PaddingTop(8)
                                                .Text(text =>
                                                {
                                                    text.Span("Mã phiếu: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.ReceiptCode);
                                                });

                                            left.Item()
                                                .PaddingTop(4)
                                                .Text(text =>
                                                {
                                                    text.Span("Ngày nhập: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.ReceiptDate
                                                            .ToString(
                                                                "dd/MM/yyyy HH:mm"));
                                                });

                                            left.Item()
                                                .PaddingTop(4)
                                                .Text(text =>
                                                {
                                                    text.Span("Người lập: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.CreatedBy);
                                                });
                                        });


                                    // RIGHT
                                    row.RelativeItem()
                                        .Column(right =>
                                        {
                                            right.Item()
                                                .Text("NHÀ CUNG CẤP")
                                                .Bold()
                                                .FontSize(10)
                                                .FontColor("#0D6EFD");

                                            right.Item()
                                                .PaddingTop(8)
                                                .Text(text =>
                                                {
                                                    text.Span("Tên: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.Supplier?.Name
                                                        ?? "-");
                                                });

                                            right.Item()
                                                .PaddingTop(4)
                                                .Text(text =>
                                                {
                                                    text.Span("Địa chỉ: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.Supplier?.Address
                                                        ?? "-");
                                                });

                                            right.Item()
                                                .PaddingTop(4)
                                                .Text(text =>
                                                {
                                                    text.Span("Điện thoại: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.Supplier?.Phone
                                                        ?? "-");
                                                });

                                            right.Item()
                                                .PaddingTop(4)
                                                .Text(text =>
                                                {
                                                    text.Span("Email: ")
                                                        .Bold();

                                                    text.Span(
                                                        receipt.Supplier?.Email
                                                        ?? "-");
                                                });
                                        });

                                });


                            column.Item()
                                .PaddingTop(20)
                                .Text("CHI TIẾT NHẬP KHO")
                                .Bold()
                                .FontSize(11)
                                .FontColor("#212529");


                            // =================================================
                            // DETAILS TABLE
                            // =================================================

                            column.Item()
                                .PaddingTop(8)
                                .Table(table =>
                                {

                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(35);
                                        columns.RelativeColumn(4);
                                        columns.ConstantColumn(55);
                                        columns.ConstantColumn(90);
                                        columns.ConstantColumn(105);
                                    });


                                    // HEADER
                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Background("#0D6EFD")
                                            .PaddingVertical(8)
                                            .AlignCenter()
                                            .Text("STT")
                                            .Bold()
                                            .FontColor("#FFFFFF");

                                        header.Cell()
                                            .Background("#0D6EFD")
                                            .PaddingVertical(8)
                                            .PaddingLeft(7)
                                            .Text("TÊN SÁCH")
                                            .Bold()
                                            .FontColor("#FFFFFF");

                                        header.Cell()
                                            .Background("#0D6EFD")
                                            .PaddingVertical(8)
                                            .AlignCenter()
                                            .Text("SL")
                                            .Bold()
                                            .FontColor("#FFFFFF");

                                        header.Cell()
                                            .Background("#0D6EFD")
                                            .PaddingVertical(8)
                                            .AlignRight()
                                            .PaddingRight(7)
                                            .Text("ĐƠN GIÁ")
                                            .Bold()
                                            .FontColor("#FFFFFF");

                                        header.Cell()
                                            .Background("#0D6EFD")
                                            .PaddingVertical(8)
                                            .AlignRight()
                                            .PaddingRight(7)
                                            .Text("THÀNH TIỀN")
                                            .Bold()
                                            .FontColor("#FFFFFF");
                                    });


                                    // DETAILS
                                    int index = 1;

                                    foreach (var detail in receipt.Details)
                                    {
                                        decimal amount =
                                            detail.Quantity *
                                            detail.UnitPrice;


                                        string background =
                                            index % 2 == 0
                                                ? "#F8F9FA"
                                                : "#FFFFFF";


                                        table.Cell()
                                            .Background(background)
                                            .BorderBottom(1)
                                            .BorderColor("#E9ECEF")
                                            .Padding(7)
                                            .AlignCenter()
                                            .Text(index.ToString());


                                        table.Cell()
                                            .Background(background)
                                            .BorderBottom(1)
                                            .BorderColor("#E9ECEF")
                                            .Padding(7)
                                            .Text(
                                                detail.Book?.Title
                                                ?? "Unknown Book")
                                            .Bold();


                                        table.Cell()
                                            .Background(background)
                                            .BorderBottom(1)
                                            .BorderColor("#E9ECEF")
                                            .Padding(7)
                                            .AlignCenter()
                                            .Text(
                                                detail.Quantity.ToString());


                                        table.Cell()
                                            .Background(background)
                                            .BorderBottom(1)
                                            .BorderColor("#E9ECEF")
                                            .Padding(7)
                                            .AlignRight()
                                            .Text(
                                                $"{detail.UnitPrice:N0}");


                                        table.Cell()
                                            .Background(background)
                                            .BorderBottom(1)
                                            .BorderColor("#E9ECEF")
                                            .Padding(7)
                                            .AlignRight()
                                            .Text(
                                                $"{amount:N0}");

                                        index++;
                                    }

                                });


                            // =================================================
                            // TOTAL
                            // =================================================

                            column.Item()
                                .PaddingTop(15)
                                .AlignRight()
                                .Column(total =>
                                {
                                    total.Item()
                                        .Text(text =>
                                        {
                                            text.Span("TỔNG SỐ LƯỢNG: ")
                                                .Bold();

                                            text.Span(
                                                receipt.Details
                                                    .Sum(x => x.Quantity)
                                                    .ToString());

                                            text.Span(" cuốn");
                                        });


                                    total.Item()
                                        .PaddingTop(5)
                                        .Background("#EAF2FF")
                                        .Border(1)
                                        .BorderColor("#0D6EFD")
                                        .Padding(10)
                                        .Row(row =>
                                        {
                                            row.AutoItem()
                                                .Text("TỔNG TIỀN")
                                                .Bold()
                                                .FontSize(11);

                                            row.AutoItem()
                                                .PaddingLeft(20)
                                                .Text(
                                                    $"{receipt.TotalAmount:N0} VNĐ")
                                                .Bold()
                                                .FontSize(15)
                                                .FontColor("#0D6EFD");
                                        });
                                });


                            // =================================================
                            // NOTE
                            // =================================================

                            if (!string.IsNullOrWhiteSpace(
                                receipt.Note))
                            {
                                column.Item()
                                    .PaddingTop(20)
                                    .Background("#FFF8E1")
                                    .Border(1)
                                    .BorderColor("#FFE082")
                                    .Padding(10)
                                    .Column(note =>
                                    {
                                        note.Item()
                                            .Text("GHI CHÚ")
                                            .Bold()
                                            .FontSize(10);

                                        note.Item()
                                            .PaddingTop(4)
                                            .Text(receipt.Note);
                                    });
                            }


                            // =================================================
                            // SIGNATURE
                            // =================================================

                            column.Item()
                                .PaddingTop(45)
                                .Row(row =>
                                {

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(left =>
                                        {
                                            left.Item()
                                                .Text("NHÀ CUNG CẤP")
                                                .Bold()
                                                .FontSize(10);

                                            left.Item()
                                                .PaddingTop(4)
                                                .Text("(Ký và ghi rõ họ tên)")
                                                .Italic()
                                                .FontSize(8)
                                                .FontColor("#6C757D");

                                            left.Item()
                                                .PaddingTop(50)
                                                .Text(
                                                    "____________________");
                                        });


                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(right =>
                                        {
                                            right.Item()
                                                .Text("NGƯỜI LẬP PHIẾU")
                                                .Bold()
                                                .FontSize(10);

                                            right.Item()
                                                .PaddingTop(4)
                                                .Text("(Ký và ghi rõ họ tên)")
                                                .Italic()
                                                .FontSize(8)
                                                .FontColor("#6C757D");

                                            right.Item()
                                                .PaddingTop(50)
                                                .Text(
                                                    "____________________");
                                        });

                                });

                        });


                    // =====================================================
                    // FOOTER
                    // =====================================================

                    page.Footer()
                        .BorderTop(1)
                        .BorderColor("#DEE2E6")
                        .PaddingTop(8)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text(
                                    $"Phiếu nhập: {receipt.ReceiptCode}")
                                .FontSize(8)
                                .FontColor("#6C757D");

                            row.RelativeItem()
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.Span("Trang ")
                                        .FontSize(8)
                                        .FontColor("#6C757D");

                                    text.CurrentPageNumber()
                                        .FontSize(8);

                                    text.Span(" / ")
                                        .FontSize(8);

                                    text.TotalPages()
                                        .FontSize(8);
                                });
                        });

                });
            });


            // =====================================================
            // RETURN PDF
            // =====================================================

            byte[] pdf =
                document.GeneratePdf();


            string fileName =
                $"PhieuNhap_{receipt.ReceiptCode}.pdf";


            return File(
                pdf,
                "application/pdf",
                fileName);
        }

        private async Task LoadEditData()
        {
            ViewBag.Suppliers = await _context.Suppliers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.Books = await _context.Books
                .OrderBy(x => x.Title)
                .ToListAsync();
        }


        // ==============================
        // LOAD CREATE DATA
        // ==============================

        private async Task LoadCreateData()
        {
            ViewBag.Suppliers =
                await _context.Suppliers
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();

            ViewBag.Books =
                await _context.Books
                    .OrderBy(x => x.Title)
                    .ToListAsync();
        }
    }
}