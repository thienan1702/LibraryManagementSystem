using LibraryManagement.Data;
using LibraryManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Services.Implementations
{
    public class PdfService : IPdfService
    {
        private readonly ApplicationDbContext _context;

        public PdfService(ApplicationDbContext context)
        {
            _context = context;

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateBorrowPdf(int borrowId)
        {
            var borrow = _context.Borrows
                .Include(x => x.BorrowDetails)
                    .ThenInclude(x => x.Book)
                .FirstOrDefault(x => x.Id == borrowId);

            if (borrow == null)
                throw new Exception("Borrow not found");

            var totalQuantity = borrow.BorrowDetails
                .Sum(x => x.Quantity);

            var generatedDate = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // =========================
                    // PAGE
                    // =========================

                    page.Size(PageSizes.A4);

                    page.MarginTop(35);
                    page.MarginBottom(35);
                    page.MarginLeft(40);
                    page.MarginRight(40);

                    // =========================
                    // HEADER
                    // =========================

                    page.Header()
                        .Column(header =>
                        {
                            header.Item()
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .Column(left =>
                                        {
                                            left.Item()
                                                .Text("LIBRARY")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor(Colors.Blue.Darken2);

                                            left.Item()
                                                .Text("MANAGEMENT SYSTEM")
                                                .FontSize(11)
                                                .Bold()
                                                .FontColor(Colors.Grey.Darken1);
                                        });

                                    row.ConstantItem(160)
                                        .AlignRight()
                                        .Column(right =>
                                        {
                                            right.Item()
                                                .Text("BORROW RECEIPT")
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor(Colors.Blue.Darken2);

                                            right.Item()
                                                .Text($"#BR-{borrow.Id:D5}")
                                                .FontSize(10)
                                                .FontColor(Colors.Grey.Darken1);
                                        });
                                });

                            header.Item()
                                .PaddingTop(12)
                                .LineHorizontal(2)
                                .LineColor(Colors.Blue.Darken2);
                        });

                    // =========================
                    // CONTENT
                    // =========================

                    page.Content()
                        .PaddingTop(20)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // =========================
                            // TITLE
                            // =========================

                            column.Item()
                                .AlignCenter()
                                .Text("BOOK BORROWING RECEIPT")
                                .FontSize(19)
                                .Bold()
                                .FontColor(Colors.Grey.Darken3);

                            column.Item()
                                .AlignCenter()
                                .Text("Official library borrowing record")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);


                            // =========================
                            // BORROWER INFORMATION
                            // =========================

                            column.Item()
                                .Background(Colors.Grey.Lighten4)
                                .Border(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .Padding(15)
                                .Column(info =>
                                {
                                    info.Item()
                                        .Text("BORROWER INFORMATION")
                                        .FontSize(11)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);

                                    info.Item()
                                        .PaddingTop(10)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Column(left =>
                                                {
                                                    left.Item()
                                                        .Text("Borrower")
                                                        .FontSize(9)
                                                        .FontColor(Colors.Grey.Darken1);

                                                    left.Item()
                                                        .Text(borrow.BorrowerName)
                                                        .FontSize(12)
                                                        .Bold();
                                                });

                                            row.RelativeItem()
                                                .Column(right =>
                                                {
                                                    right.Item()
                                                        .Text("Borrow Date")
                                                        .FontSize(9)
                                                        .FontColor(Colors.Grey.Darken1);

                                                    right.Item()
                                                        .Text(
                                                            borrow.BorrowDate
                                                                .ToString("dd/MM/yyyy"))
                                                        .FontSize(12)
                                                        .Bold();
                                                });
                                        });

                                    info.Item()
                                        .PaddingTop(12)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Column(left =>
                                                {
                                                    left.Item()
                                                        .Text("Due Date")
                                                        .FontSize(9)
                                                        .FontColor(Colors.Grey.Darken1);

                                                    left.Item()
                                                        .Text(
                                                            borrow.DueDate
                                                                .ToString("dd/MM/yyyy"))
                                                        .FontSize(12)
                                                        .Bold()
                                                        .FontColor(Colors.Red.Darken1);
                                                });

                                            row.RelativeItem()
                                                .Column(right =>
                                                {
                                                    right.Item()
                                                        .Text("Return Date")
                                                        .FontSize(9)
                                                        .FontColor(Colors.Grey.Darken1);

                                                    right.Item()
                                                        .Text(
                                                            borrow.ReturnDate.HasValue
                                                                ? borrow.ReturnDate.Value
                                                                    .ToString("dd/MM/yyyy")
                                                                : "Not returned")
                                                        .FontSize(12)
                                                        .Bold();
                                                });
                                        });
                                });


                            // =========================
                            // BOOK LIST TITLE
                            // =========================

                            column.Item()
                                .PaddingTop(5)
                                .Text("BORROWED BOOKS")
                                .FontSize(11)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);


                            // =========================
                            // BOOK TABLE
                            // =========================

                            column.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(45);

                                        columns.RelativeColumn(4);

                                        columns.RelativeColumn(2);

                                        columns.ConstantColumn(70);
                                    });


                                    // HEADER
                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .AlignCenter()
                                            .Text("#");

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Book Title");

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .Text("Book ID");

                                        header.Cell()
                                            .Element(HeaderCellStyle)
                                            .AlignCenter()
                                            .Text("Quantity");
                                    });


                                    int index = 1;

                                    foreach (var item in borrow.BorrowDetails)
                                    {
                                        var background =
                                            index % 2 == 0
                                                ? Colors.Grey.Lighten5
                                                : Colors.White;

                                        table.Cell()
                                            .Background(background)
                                            .Element(BodyCellStyle)
                                            .AlignCenter()
                                            .Text(index.ToString());

                                        table.Cell()
                                            .Background(background)
                                            .Element(BodyCellStyle)
                                            .Text(item.Book?.Title ?? "Unknown");

                                        table.Cell()
                                            .Background(background)
                                            .Element(BodyCellStyle)
                                            .Text(item.BookId.ToString());

                                        table.Cell()
                                            .Background(background)
                                            .Element(BodyCellStyle)
                                            .AlignCenter()
                                            .Text(item.Quantity.ToString());

                                        index++;
                                    }
                                });


                            // =========================
                            // SUMMARY
                            // =========================

                            column.Item()
                                .AlignRight()
                                .PaddingTop(5)
                                .Row(row =>
                                {
                                    row.AutoItem()
                                        .Text("Total books: ")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);

                                    row.AutoItem()
                                        .Text(totalQuantity.ToString())
                                        .FontSize(11)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);
                                });


                            // =========================
                            // NOTE
                            // =========================

                            column.Item()
                                .PaddingTop(15)
                                .Background(Colors.Blue.Lighten5)
                                .BorderLeft(4)
                                .BorderColor(Colors.Blue.Darken2)
                                .Padding(12)
                                .Column(note =>
                                {
                                    note.Item()
                                        .Text("IMPORTANT")
                                        .FontSize(9)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2);

                                    note.Item()
                                        .PaddingTop(4)
                                        .Text(
                                            "Please return all borrowed books before the due date. " +
                                            "Late returns may result in an overdue fine.")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken2);
                                });


                            // =========================
                            // SIGNATURE
                            // =========================

                            column.Item()
                                .PaddingTop(35)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(signature =>
                                        {
                                            signature.Item()
                                                .Text("Borrower")
                                                .FontSize(10)
                                                .Bold();

                                            signature.Item()
                                                .PaddingTop(45)
                                                .Text("(Signature)")
                                                .FontSize(9)
                                                .FontColor(
                                                    Colors.Grey.Darken1);
                                        });


                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(signature =>
                                        {
                                            signature.Item()
                                                .Text("Librarian")
                                                .FontSize(10)
                                                .Bold();

                                            signature.Item()
                                                .PaddingTop(45)
                                                .Text("Library Management")
                                                .FontSize(9)
                                                .FontColor(
                                                    Colors.Grey.Darken1);
                                        });
                                });
                        });


                    // =========================
                    // FOOTER
                    // =========================

                    page.Footer()
                        .PaddingTop(10)
                        .BorderTop(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .AlignLeft()
                                .Text(
                                    $"Generated: {generatedDate:dd/MM/yyyy HH:mm}")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            row.RelativeItem()
                                .AlignRight()
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

            }).GeneratePdf();
        }


        // =========================
        // TABLE HEADER STYLE
        // =========================

        static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Blue.Darken2)
                .PaddingVertical(8)
                .PaddingHorizontal(7)
                .BorderBottom(1)
                .BorderColor(Colors.Blue.Darken3)
                .DefaultTextStyle(
                    x => x
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.White));
        }


        // =========================
        // TABLE BODY STYLE
        // =========================

        static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .PaddingVertical(8)
                .PaddingHorizontal(7)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .DefaultTextStyle(
                    x => x
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken3));
        }
    }
}