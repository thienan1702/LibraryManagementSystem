using ClosedXML.Excel;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp.Rendering;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditService _audit;
        private readonly BarcodeService _barcodeService;


        public BooksController(
            ApplicationDbContext context,
            IWebHostEnvironment environment, IAuditService audit, BarcodeService barcodeService)
        {
            _context = context;
            _environment = environment;
            _audit = audit;
            _barcodeService = barcodeService;
        }

        // GET: Books

        public IActionResult Index(
    string search,
    int? categoryId,
    int? authorId,
    int? publisherId,
    string sortOrder,
    int? page)
        {
            ViewBag.TitleSort = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewBag.QuantitySort = sortOrder == "quantity" ? "quantity_desc" : "quantity";

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.AuthorId = authorId;
            ViewBag.PublisherId = publisherId;
            ViewBag.SortOrder = sortOrder;

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryId);
            ViewBag.Authors = new SelectList(_context.Authors, "Id", "Name", authorId);
            ViewBag.Publishers = new SelectList(_context.Publishers, "Id", "Name", publisherId);

            var books = _context.Books
                .Include(x => x.Category)
                .Include(x => x.Author)
                .Include(x => x.Publisher)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                books = books.Where(x => x.Title.Contains(search));
            }

            if (categoryId.HasValue)
            {
                books = books.Where(x => x.CategoryId == categoryId);
            }

            if (authorId.HasValue)
            {
                books = books.Where(x => x.AuthorId == authorId);
            }

            if (publisherId.HasValue)
            {
                books = books.Where(x => x.PublisherId == publisherId);
            }

            switch (sortOrder)
            {
                case "title_desc":
                    books = books.OrderByDescending(x => x.Title);
                    break;

                case "quantity":
                    books = books.OrderBy(x => x.Quantity);
                    break;

                case "quantity_desc":
                    books = books.OrderByDescending(x => x.Quantity);
                    break;

                default:
                    books = books.OrderBy(x => x.Title);
                    break;
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;

            return View(books.ToPagedList(pageNumber, pageSize));
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var book = await _context.Books
             .Include(x => x.Author)
             .Include(x => x.Category)
             .Include(x => x.Publisher)
             .Include(x => x.BorrowDetails)
                 .ThenInclude(x => x.Borrow)
             .FirstOrDefaultAsync(x => x.Id == id);

            if (book == null)
                return NotFound(); 

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name");
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "Id", "Name");

            return View();
        }

        // POST: Books/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ISBN,Quantity,AvailableQuantity,Description,ImageUrl,ImageFile,CategoryId,AuthorId,PublisherId")] Book book)
        {
            if (_context.Books.Any(x => x.ISBN == book.ISBN))
            {
                ModelState.AddModelError("ISBN", "This ISBN already exists.");
            }
            if (ModelState.IsValid)
            {
                book.ImageUrl = await UploadImage(book.ImageFile);
                book.AvailableQuantity = book.Quantity;
                _context.Add(book);
                await _context.SaveChangesAsync();
                await _audit.SaveAsync(
                    User.Identity?.Name ?? "System",
                    "Create",
                    "Book",
                    book.Id,
                    $"Created book '{book.Title}'");

                TempData["Success"] = "Book added successfully.";

                return RedirectToAction(nameof(Index));
            }
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            ViewData["AuthorId"] = new SelectList(_context.Authors, "Id", "Name", book.AuthorId);
            ViewData["PublisherId"] = new SelectList(_context.Publishers, "Id", "Name", book.PublisherId);
            return View(book);
        }

        // POST: Books/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,ISBN,Quantity,Description,ImageUrl,ImageFile,CategoryId,AuthorId,PublisherId")]
    Book book)
        {
            if (id != book.Id)
            {
                return NotFound();
            }

            // Không cho Quantity âm
            if (book.Quantity < 0)
            {
                ModelState.AddModelError(
                    "Quantity",
                    "Quantity cannot be negative.");
            }

            // Kiểm tra ISBN trùng
            if (_context.Books.Any(x =>
                x.ISBN == book.ISBN &&
                x.Id != book.Id))
            {
                ModelState.AddModelError(
                    "ISBN",
                    "This ISBN already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBook = await _context.Books
                        .FirstOrDefaultAsync(x => x.Id == id);

                    if (existingBook == null)
                    {
                        return NotFound();
                    }

                    // Số sách đang được mượn
                    int borrowedQuantity =
                        existingBook.Quantity -
                        existingBook.AvailableQuantity;

                    // Không cho Quantity nhỏ hơn số sách đang được mượn
                    if (book.Quantity < borrowedQuantity)
                    {
                        ModelState.AddModelError(
                            "Quantity",
                            $"Quantity cannot be less than {borrowedQuantity} because these books are currently borrowed.");

                        ViewData["AuthorId"] = new SelectList(
                            _context.Authors,
                            "Id",
                            "Name",
                            book.AuthorId);

                        ViewData["CategoryId"] = new SelectList(
                            _context.Categories,
                            "Id",
                            "Name",
                            book.CategoryId);

                        ViewData["PublisherId"] = new SelectList(
                            _context.Publishers,
                            "Id",
                            "Name",
                            book.PublisherId);

                        return View(book);
                    }

                    // Giữ lại số sách đang được mượn
                    int newAvailableQuantity =
                        book.Quantity - borrowedQuantity;

                    existingBook.Title = book.Title;
                    existingBook.ISBN = book.ISBN;
                    existingBook.Quantity = book.Quantity;
                    existingBook.AvailableQuantity = newAvailableQuantity;
                    existingBook.Description = book.Description;
                    existingBook.CategoryId = book.CategoryId;
                    existingBook.AuthorId = book.AuthorId;
                    existingBook.PublisherId = book.PublisherId;

                    // Upload ảnh mới
                    if (book.ImageFile != null)
                    {
                        DeleteImage(existingBook.ImageUrl);

                        existingBook.ImageUrl =
                            await UploadImage(book.ImageFile);
                    }

                    await _context.SaveChangesAsync();

                    await _audit.SaveAsync(
                        User.Identity?.Name ?? "System",
                        "Edit",
                        "Book",
                        existingBook.Id,
                        $"Updated book '{existingBook.Title}'");

                    TempData["Success"] =
                        "Book updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
            }

            ViewData["AuthorId"] = new SelectList(
                _context.Authors,
                "Id",
                "Name",
                book.AuthorId);

            ViewData["CategoryId"] = new SelectList(
                _context.Categories,
                "Id",
                "Name",
                book.CategoryId);

            ViewData["PublisherId"] = new SelectList(
                _context.Publishers,
                "Id",
                "Name",
                book.PublisherId);

            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .Include(b => b.BorrowDetails)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            // Không cho xóa sách đã từng được mượn
            if (book.BorrowDetails != null &&
                book.BorrowDetails.Any())
            {
                TempData["Error"] =
                    "Cannot delete this book because it has borrow history.";

                return RedirectToAction(nameof(Index));
            }

            _context.Books.Remove(book);

            await _context.SaveChangesAsync();

            await _audit.SaveAsync(
                User.Identity?.Name ?? "System",
                "Delete",
                "Book",
                book.Id,
                $"Deleted book '{book.Title}'");

            TempData["Success"] = "Book deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        private async Task<string> UploadImage(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "/images/books/no-image.png";

            string extension = Path.GetExtension(file.FileName).ToLower();

            string[] allow =
            {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
            };

            if (!allow.Contains(extension))
                return "/images/books/no-image.png";

            if (file.Length > 2 * 1024 * 1024)
                return "/images/books/no-image.png";

            string folder = Path.Combine(
                _environment.WebRootPath,
                "images",
                "books");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid() + extension;

            string path =
                Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);

            await file.CopyToAsync(stream);

            return "/images/books/" + fileName;
        }

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            if (imageUrl.Contains("no-image"))
                return;

            string path = Path.Combine(
                _environment.WebRootPath,
                imageUrl.TrimStart('/').Replace("/", "\\"));

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        public IActionResult ExportExcel()
        {
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Books");

            worksheet.Cell(1, 1).Value = "Title";
            worksheet.Cell(1, 2).Value = "Author";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Publisher";
            worksheet.Cell(1, 5).Value = "Quantity";
            worksheet.Cell(1, 6).Value = "Available";

            int row = 2;

            var books = _context.Books
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Include(x => x.Publisher)
                .ToList();

            foreach (var book in books)
            {
                worksheet.Cell(row, 1).Value = book.Title;
                worksheet.Cell(row, 2).Value = book.Author?.Name;
                worksheet.Cell(row, 3).Value = book.Category?.Name;
                worksheet.Cell(row, 4).Value = book.Publisher?.Name;
                worksheet.Cell(row, 5).Value = book.Quantity;
                worksheet.Cell(row, 6).Value = book.AvailableQuantity;

                row++;
            }

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Books.xlsx");
        }

        public IActionResult ExportPdf()
        {
            var books = _context.Books
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Include(x => x.Publisher)
                .ToList();

            return new ViewAsPdf("PdfBooks", books)
            {
                FileName = "Books.pdf",
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape
            };
        }


        public IActionResult Barcode(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return BadRequest();

            var writer = new BarcodeWriter<SKBitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 500,
                    Height = 120,
                    Margin = 10,
                    PureBarcode = false
                },
                Renderer = new SKBitmapRenderer()
            };

            using var bitmap = writer.Write(isbn);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return File(data.ToArray(), "image/png");
        }



        [HttpGet]
        public async Task<IActionResult> FindByISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return Json(null);

            var book = await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(x => x.ISBN == isbn);

            if (book == null)
                return Json(null);

            return Json(new
            {
                id = book.Id,
                title = book.Title,
                isbn = book.ISBN,
                available = book.AvailableQuantity,
                quantity = book.Quantity,
                author = book.Author?.Name,
                category = book.Category?.Name,
                publisher = book.Publisher?.Name,
                image = book.ImageUrl
            });
        }



        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}
