using ClosedXML.Excel;
using LibraryManagement.Data;
using LibraryManagement.Helpers;
using LibraryManagement.Models;
using LibraryManagement.Reports;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace LibraryManagement.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> ExportPdf()
        {
            var borrows = await _context.Borrows
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            var document = new BorrowReportPdf(borrows);

            byte[] pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Borrow_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Borrow Report
        public async Task<IActionResult> BorrowReport(
            DateTime? fromDate,
            DateTime? toDate,
            string? keyword)
        {
            var query = _context.Borrows.AsQueryable();

            if (fromDate != null)
                query = query.Where(x => x.BorrowDate >= fromDate);

            if (toDate != null)
                query = query.Where(x => x.BorrowDate <= toDate);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.BorrowerName.Contains(keyword) ||
                    x.BorrowerEmail.Contains(keyword));
            }

            BorrowReportVM vm = new()
            {
                FromDate = fromDate,
                ToDate = toDate,
                Keyword = keyword,
                Borrows = await query
                    .OrderByDescending(x => x.BorrowDate)
                    .ToListAsync()
            };

            return View(vm);
        }


        public async Task<IActionResult> ExportExcel()
        {
            var borrows = await _context.Borrows
                .Include(x => x.BorrowDetails)
                .ThenInclude(x => x.Book)
                .OrderByDescending(x => x.BorrowDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Borrow Report");

            //==========================
            // TITLE
            //==========================

            ws.Cell("A1").Value = "LIBRARY MANAGEMENT";
            ws.Cell("A2").Value = "Borrow Report";
            ws.Cell("A3").Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Range("A1:H1").Merge();
            ws.Range("A2:H2").Merge();
            ws.Range("A3:H3").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontSize = 10;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;
            ws.Cell("A3").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

         

            //==========================
            // HEADER
            //==========================

            ws.Cell(5, 1).Value = "No";
            ws.Cell(5, 2).Value = "Borrower";
            ws.Cell(5, 3).Value = "Email";
            ws.Cell(5, 4).Value = "Borrow Date";
            ws.Cell(5, 5).Value = "Due Date";
            ws.Cell(5, 6).Value = "Status";
            ws.Cell(5, 7).Value = "Fine";
            ws.Cell(5, 8).Value = "Books";

            var header = ws.Range("A5:H5");

            header.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================
            // DATA
            //==========================

            int row = 6;
            int stt = 1;

            foreach (var item in borrows)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = item.BorrowerName;
                ws.Cell(row, 3).Value = item.BorrowerEmail;
                ws.Cell(row, 4).Value = item.BorrowDate;
                ws.Cell(row, 5).Value = item.DueDate;

                ws.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                string status;

                if (!item.IsReturned)
                    status = "Borrowing";
                else if (item.FineAmount > 0)
                    status = "Returned (Fine)";
                else
                    status = "Returned";

                ws.Cell(row, 6).Value = status;

                ws.Cell(row, 7).Value = item.FineAmount;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                ws.Cell(row, 8).Value =
                    string.Join(", ",
                        item.BorrowDetails.Select(x =>
                            $"{x.Book.Title} ({x.Quantity})"));

                row++;
            }

            //==========================
            // BORDER
            //==========================

            ws.Range($"A5:H{row - 1}")
                .Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

            ws.Range($"A5:H{row - 1}")
                .Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

            //==========================
            // AUTO SIZE
            //==========================

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Borrow_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }



        public async Task<IActionResult> ExportInventoryExcel(string? keyword)
        {
            var query = _context.Books
                .Include(x => x.Author)
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Author.Name.Contains(keyword) ||
                    x.Category.Name.Contains(keyword));
            }

            var books = await query
                .OrderBy(x => x.Title)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Inventory Report");

            //================ TITLE =================

            ws.Cell("A1").Value = "LIBRARY MANAGEMENT SYSTEM";
            ws.Cell("A2").Value = "INVENTORY REPORT";

            ws.Range("A1:G1").Merge();
            ws.Range("A2:G2").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range("A3:G3").Merge();

            //================ HEADER =================

            ws.Cell(5, 1).Value = "No";
            ws.Cell(5, 2).Value = "Title";
            ws.Cell(5, 3).Value = "Author";
            ws.Cell(5, 4).Value = "Category";
            ws.Cell(5, 5).Value = "Total";
            ws.Cell(5, 6).Value = "Available";
            ws.Cell(5, 7).Value = "Status";

            var header = ws.Range("A5:G5");

            header.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            //================ DATA =================

            int row = 6;
            int stt = 1;

            foreach (var book in books)
            {
                string status;

                if (book.AvailableQuantity == 0)
                    status = "Out of Stock";
                else if (book.AvailableQuantity <= 3)
                    status = "Low Stock";
                else
                    status = "Available";

                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = book.Title;
                ws.Cell(row, 3).Value = book.Author.Name;
                ws.Cell(row, 4).Value = book.Category.Name;
                ws.Cell(row, 5).Value = book.Quantity;
                ws.Cell(row, 6).Value = book.AvailableQuantity;
                ws.Cell(row, 7).Value = status;

                row++;
            }

            //================ SUMMARY =================

            ws.Cell(row + 2, 1).Value = "SUMMARY";
            ws.Cell(row + 2, 1).Style.Font.Bold = true;

            ws.Cell(row + 3, 1).Value = "Total Titles";
            ws.Cell(row + 3, 2).Value = books.Count;

            ws.Cell(row + 4, 1).Value = "Total Books";
            ws.Cell(row + 4, 2).Value = books.Sum(x => x.Quantity);

            ws.Cell(row + 5, 1).Value = "Available";
            ws.Cell(row + 5, 2).Value = books.Sum(x => x.AvailableQuantity);

            ws.Cell(row + 6, 1).Value = "Borrowed";
            ws.Cell(row + 6, 2).Value = books.Sum(x => x.Quantity - x.AvailableQuantity);

            ws.Cell(row + 7, 1).Value = "Low Stock";
            ws.Cell(row + 7, 2).Value = books.Count(x => x.AvailableQuantity <= 3);

            ws.Cell(row + 8, 1).Value = "Out Of Stock";
            ws.Cell(row + 8, 2).Value = books.Count(x => x.AvailableQuantity == 0);

            //================ BORDER =================

            ws.Range($"A5:G{row - 1}")
                .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            ws.Range($"A5:G{row - 1}")
                .Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            //================ AUTO FIT =================

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventory_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }


        // Fine Report
        public async Task<IActionResult> FineReport(
      DateTime? fromDate,
      DateTime? toDate,
      string? keyword)
        {
            var data = await GetFineQuery(
                fromDate,
                toDate,
                keyword)
                .OrderByDescending(x => x.FineAmount)
                .ToListAsync();

            FineReportVM vm = new()
            {
                FromDate = fromDate,
                ToDate = toDate,
                Keyword = keyword,
                Borrows = data
            };

            return View(vm);
        }


        public async Task<IActionResult> ExportFineExcel(
     DateTime? fromDate,
     DateTime? toDate,
     string? keyword)
        {
            var borrows = await GetFineQuery(
                fromDate,
                toDate,
                keyword)
                .OrderByDescending(x => x.FineAmount)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Fine Report");

            //==========================
            // TITLE
            //==========================

            ws.Cell("A1").Value = "LIBRARY MANAGEMENT";
            ws.Cell("A2").Value = "Fine Report";
            ws.Cell("A3").Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Range("A1:F1").Merge();
            ws.Range("A2:F2").Merge();
            ws.Range("A3:F3").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontSize = 10;
            ws.Cell("A3").Style.Font.FontColor = XLColor.Gray;
            ws.Cell("A3").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            //==========================
            // HEADER
            //==========================

            ws.Cell(5, 1).Value = "No";
            ws.Cell(5, 2).Value = "Borrower";
            ws.Cell(5, 3).Value = "Email";
            ws.Cell(5, 4).Value = "Borrow Date";
            ws.Cell(5, 5).Value = "Due Date";
            ws.Cell(5, 6).Value = "Fine";

            var header = ws.Range("A5:F5");

            header.Style.Fill.BackgroundColor = XLColor.DarkRed;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================
            // DATA
            //==========================

            int row = 6;
            int no = 1;

            foreach (var item in borrows)
            {
                ws.Cell(row, 1).Value = no++;
                ws.Cell(row, 2).Value = item.BorrowerName;
                ws.Cell(row, 3).Value = item.BorrowerEmail;

                ws.Cell(row, 4).Value = item.BorrowDate;
                ws.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";

                ws.Cell(row, 5).Value = item.DueDate;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                ws.Cell(row, 6).Value = item.FineAmount;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";

                // Tô màu nếu có tiền phạt cao
                if (item.FineAmount >= 500000)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                }
                else if (item.FineAmount >= 100000)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                row++;
            }

            //==========================
            // BORDER
            //==========================

            ws.Range($"A5:F{row - 1}")
                .Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

            ws.Range($"A5:F{row - 1}")
                .Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

            //==========================
            // AUTO SIZE
            //==========================

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Fine_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        public async Task<IActionResult> ExportFinePdf(
    DateTime? fromDate,
    DateTime? toDate,
    string? keyword)
        {
            var borrows = await GetFineQuery(
                fromDate,
                toDate,
                keyword)
                .OrderByDescending(x => x.FineAmount)
                .ToListAsync();

            var document = new FineReportPdf(borrows);

            byte[] pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Fine_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }


        private IQueryable<Borrow> GetFineQuery(
    DateTime? fromDate,
    DateTime? toDate,
    string? keyword)
        {
            var query = _context.Borrows
                .Include(x => x.BorrowDetails)
                .ThenInclude(x => x.Book)
                .Where(x => x.FineAmount > 0)
                .AsQueryable();

            if (fromDate != null)
                query = query.Where(x => x.BorrowDate >= fromDate);

            if (toDate != null)
                query = query.Where(x => x.BorrowDate <= toDate);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.BorrowerName.Contains(keyword) ||
                    x.BorrowerEmail.Contains(keyword));
            }

            return query;
        }


        public async Task<IActionResult> ExportInventoryPdf(string? keyword)
        {
            var query = _context.Books
                .Include(x => x.Author)
                .Include(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Author.Name.Contains(keyword) ||
                    x.Category.Name.Contains(keyword));
            }

            var books = await query
                .OrderBy(x => x.Title)
                .ToListAsync();

            var document = new InventoryReportPdf(books);

            var pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Inventory_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Inventory Report
        public async Task<IActionResult> InventoryReport(string? keyword)
        {
            var query = _context.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Author.Name.Contains(keyword) ||
                    x.Category.Name.Contains(keyword));
            }

            var books = await query
                .Include(x => x.Author)
                .Include(x => x.Category)
                .OrderBy(x => x.Title)
                .ToListAsync();

            InventoryReportVM vm = new()
            {
                Keyword = keyword,

                Books = books,

                TotalTitles = books.Count,

                TotalBooks = books.Sum(x => x.Quantity),

                AvailableBooks = books.Sum(x => x.AvailableQuantity),

                BorrowedBooks =
                    books.Sum(x => x.Quantity - x.AvailableQuantity),

                LowStock =
                    books.Count(x => x.AvailableQuantity <= 3),

                OutOfStock =
                    books.Count(x => x.AvailableQuantity == 0)
            };

            return View(vm);
        }

        // Overdue Report
        public async Task<IActionResult> OverdueReport(
     string? keyword)
        {
            var query = _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                .Where(x => !x.IsReturned &&
                            x.DueDate < DateTime.Today);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.BorrowerName.Contains(keyword) ||
                    x.BorrowerEmail.Contains(keyword));
            }

            var borrows = await query
                .OrderByDescending(x => x.DueDate)
                .ToListAsync();

            return View(borrows);
        }

        public async Task<IActionResult> ExportOverduePdf(string? keyword)
        {
            var query = _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                .Where(x => !x.IsReturned &&
                            x.DueDate < DateTime.Today);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.BorrowerName.Contains(keyword) ||
                    x.BorrowerEmail.Contains(keyword));
            }

            var borrows = await query
                .OrderByDescending(x => x.DueDate)
                .ToListAsync();

            var document = new OverdueReportPdf(borrows);

            var pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"OverdueReport_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }
        public async Task<IActionResult> ExportOverdueExcel(string? keyword)
        {
            var query = _context.Borrows
                .Include(x => x.BorrowDetails)
                .ThenInclude(x => x.Book)
                .Where(x => !x.IsReturned &&
                            x.DueDate < DateTime.Today);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.BorrowerName.Contains(keyword) ||
                    x.BorrowerEmail.Contains(keyword));
            }

            var borrows = await query
                .OrderByDescending(x => x.DueDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Overdue Report");

            //==========================
            // TITLE
            //==========================

            ws.Cell("A1").Value = "LIBRARY MANAGEMENT";
            ws.Cell("A2").Value = "Overdue Report";
            ws.Cell("A3").Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Range("A1:G1").Merge();
            ws.Range("A2:G2").Merge();
            ws.Range("A3:G3").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;

            //==========================
            // HEADER
            //==========================

            ws.Cell(5, 1).Value = "No";
            ws.Cell(5, 2).Value = "Borrower";
            ws.Cell(5, 3).Value = "Email";
            ws.Cell(5, 4).Value = "Books";
            ws.Cell(5, 5).Value = "Due Date";
            ws.Cell(5, 6).Value = "Late Days";
            ws.Cell(5, 7).Value = "Fine";

            var header = ws.Range("A5:G5");

            header.Style.Fill.BackgroundColor = XLColor.DarkRed;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================
            // DATA
            //==========================

            int row = 6;
            int stt = 1;

            foreach (var item in borrows)
            {
                ws.Cell(row, 1).Value = stt++;

                ws.Cell(row, 2).Value = item.BorrowerName;

                ws.Cell(row, 3).Value = item.BorrowerEmail;

                ws.Cell(row, 4).Value =
                    string.Join(", ",
                        item.BorrowDetails
                            .Where(x => x.Book != null)
                            .Select(x => $"{x.Book!.Title} ({x.Quantity})"));

                ws.Cell(row, 5).Value = item.DueDate;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                ws.Cell(row, 6).Value = item.OverdueDays;

                ws.Cell(row, 7).Value = item.FineAmount;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";

                // tô màu nếu quá hạn lâu
                if (item.OverdueDays >= 30)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightPink;
                }
                else if (item.OverdueDays >= 15)
                {
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

                row++;
            }

            //==========================
            // BORDER
            //==========================

            ws.Range($"A5:G{row - 1}")
                .Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

            ws.Range($"A5:G{row - 1}")
                .Style.Border.InsideBorder =
                    XLBorderStyleValues.Thin;

            //==========================
            // AUTO SIZE
            //==========================

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Overdue_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        // Top Borrowed Books
        public IActionResult TopBooksReport()
        {
            return View();
        }



        public async Task<IActionResult> ReservationReport()
        {
            var reservations = await _context.Reservations
                .Include(x => x.Book)
                .OrderByDescending(x => x.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }


        public async Task<IActionResult> ExportReservationPdf()
        {
            var reservations = await _context.Reservations
                .Include(x => x.Book)
                .OrderByDescending(x => x.ReservationDate)
                .ToListAsync();

            var document =
                new ReservationReportPdf(reservations);

            var pdf = document.GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"Reservation_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }

        public async Task<IActionResult> ExportReservationExcel(
     DateTime? fromDate,
     DateTime? toDate,
     string? keyword)
        {
            var reservations = await GetReservationQuery(fromDate, toDate, keyword)
                .Include(x => x.Book)
                .OrderByDescending(x => x.ReservationDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Reservation Report");

            //==========================================================
            // TITLE
            //==========================================================

            ws.Cell("A1").Value = "LIBRARY MANAGEMENT";
            ws.Cell("A2").Value = "Reservation Report";
            ws.Cell("A3").Value = $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}";

            ws.Range("A1:G1").Merge();
            ws.Range("A2:G2").Merge();
            ws.Range("A3:G3").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            //==========================================================
            // HEADER
            //==========================================================

            ws.Cell(5, 1).Value = "No";
            ws.Cell(5, 2).Value = "Customer";
            ws.Cell(5, 3).Value = "Email";
            ws.Cell(5, 4).Value = "Book";
            ws.Cell(5, 5).Value = "Quantity";
            ws.Cell(5, 6).Value = "Reservation Date";
            ws.Cell(5, 7).Value = "Status";

            var header = ws.Range("A5:G5");

            header.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================================================
            // DATA
            //==========================================================

            int row = 6;
            int no = 1;

            foreach (var item in reservations)
            {
                ws.Cell(row, 1).Value = no++;

                ws.Cell(row, 2).Value = item.CustomerName;

                ws.Cell(row, 3).Value = item.CustomerEmail;

                ws.Cell(row, 4).Value = item.Book?.Title ?? "";

                ws.Cell(row, 5).Value = item.Quantity;

                ws.Cell(row, 6).Value = item.ReservationDate;
                ws.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy";

                ws.Cell(row, 7).Value = item.Status.ToString();

                switch (item.Status)
                {
                    case ReservationStatus.Completed:
                        ws.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
                        break;

                    case ReservationStatus.Waiting:
                        ws.Cell(row, 7).Style.Font.FontColor = XLColor.Orange;
                        break;

                    case ReservationStatus.Approved:
                        ws.Cell(row, 7).Style.Font.FontColor = XLColor.Blue;
                        break;

                    case ReservationStatus.Cancelled:
                    case ReservationStatus.Rejected:
                        ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
                        break;
                }

                row++;
            }

            //==========================================================
            // BORDER
            //==========================================================

            ws.Range($"A5:G{row - 1}")
                .Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            ws.Range($"A5:G{row - 1}")
                .Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            //==========================================================
            // AUTO SIZE
            //==========================================================

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Reservation_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        private IQueryable<Reservation> GetReservationQuery(
    DateTime? fromDate,
    DateTime? toDate,
    string? keyword)
        {
            var query = _context.Reservations
                .Include(x => x.Book)
                .AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(x =>
                    x.ReservationDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x =>
                    x.ReservationDate.Date <= toDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(x =>
                    x.CustomerName.Contains(keyword) ||
                    x.CustomerEmail.Contains(keyword) ||
                    x.Book.Title.Contains(keyword));
            }

            return query;
        }

    }
}