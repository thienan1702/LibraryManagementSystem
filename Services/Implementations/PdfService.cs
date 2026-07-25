using LibraryManagement.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LibraryManagement.Services.Interfaces;

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

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Size(PageSizes.A4);

                    page.Header().Text("LIBRARY MANAGEMENT")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text($"Borrower : {borrow.BorrowerName}");

                        column.Item().Text($"Borrow Date : {borrow.BorrowDate:dd/MM/yyyy}");

                        column.Item().Text($"Return Date : {(borrow.ReturnDate.HasValue ? borrow.ReturnDate.Value.ToString("dd/MM/yyyy") : "-")}");

                        column.Item().PaddingVertical(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(50);

                                columns.RelativeColumn();

                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");

                                header.Cell().Element(CellStyle).Text("Book");

                                header.Cell().Element(CellStyle).Text("Qty");
                            });

                            int i = 1;

                            foreach (var item in borrow.BorrowDetails)
                            {
                                table.Cell().Element(CellStyle)
                                    .Text(i++);

                                table.Cell().Element(CellStyle)
                                    .Text(item.Book.Title);

                                table.Cell().Element(CellStyle)
                                    .Text(item.Quantity.ToString());
                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container
                                    .Border(1)
                                    .Padding(5);
                            }
                        });

                        column.Item().PaddingTop(30);

                        column.Item().AlignRight().Column(c =>
                        {
                            c.Item().Text("Librarian");

                            c.Item().Height(60);

                            c.Item().Text("Library Management");
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated at ");

                            x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        });
                });

            }).GeneratePdf();
        }
    }
}