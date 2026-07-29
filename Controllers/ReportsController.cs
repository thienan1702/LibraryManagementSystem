using ClosedXML.Excel;
using LibraryManagement.Data;
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

            ws.Range("A1:H1").Merge();
            ws.Range("A2:H2").Merge();

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 14;
            ws.Cell("A2").Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================
            // HEADER
            //==========================

            ws.Cell(4, 1).Value = "No";
            ws.Cell(4, 2).Value = "Borrower";
            ws.Cell(4, 3).Value = "Email";
            ws.Cell(4, 4).Value = "Borrow Date";
            ws.Cell(4, 5).Value = "Due Date";
            ws.Cell(4, 6).Value = "Status";
            ws.Cell(4, 7).Value = "Fine";
            ws.Cell(4, 8).Value = "Books";

            var header = ws.Range("A4:H4");

            header.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            //==========================
            // DATA
            //==========================

            int row = 5;
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

            ws.Range($"A4:H{row - 1}")
                .Style.Border.OutsideBorder =
                    XLBorderStyleValues.Thin;

            ws.Range($"A4:H{row - 1}")
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

      

        // Fine Report
        public IActionResult FineReport()
        {
            return View();
        }

        // Inventory Report
        public IActionResult InventoryReport()
        {
            return View();
        }

        // Overdue Report
        public IActionResult OverdueReport()
        {
            return View();
        }

        // Top Borrowed Books
        public IActionResult TopBooksReport()
        {
            return View();
        }
    }
}