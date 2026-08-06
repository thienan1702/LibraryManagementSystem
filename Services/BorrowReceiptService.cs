using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Services
{
    public class BorrowReceiptService
    {
        public byte[] Generate(Borrow borrow)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
            container.Page(page =>
            {
            page.Margin(30);

            page.Size(PageSizes.A4);

            page.DefaultTextStyle(x => x.FontSize(11));

            page.Header().Column(column =>
            {
                column.Item().AlignCenter().Text("LIBRARY MANAGEMENT")
                    .FontSize(22)
                    .Bold();

                column.Item().AlignCenter().Text("Borrow Receipt")
                    .FontSize(15);

                column.Item().PaddingTop(10);

                column.Item().LineHorizontal(1);
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
            column.Spacing(8);

            column.Item().Text($"Borrow ID : {borrow.Id}");

            column.Item().Text($"Borrower : {borrow.BorrowerName}");

            column.Item().Text($"Email : {borrow.BorrowerEmail}");

            column.Item().Text($"Borrow Date : {borrow.BorrowDate:dd/MM/yyyy}");

            column.Item().Text($"Due Date : {borrow.DueDate:dd/MM/yyyy}");

            column.Item().PaddingVertical(10);

            column.Item().Table(table =>
            {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);

                columns.RelativeColumn(5);

                columns.ConstantColumn(80);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).Padding(5)
                    .Text("#").Bold();

                header.Cell().Border(1).Padding(5)
                    .Text("Book").Bold();

                header.Cell().Border(1).Padding(5)
                    .AlignCenter()
                    .Text("Qty").Bold();
            });

            int stt = 1;
                foreach (var item in borrow.BorrowDetails)
                {
                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .Text(stt.ToString());

                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .Text(item.Book?.Title ?? "");

                    table.Cell()
                        .Border(1)
                        .Padding(5)
                        .AlignCenter()
                        .Text(item.Quantity.ToString());

                    stt++;
                }

            });

                column.Item().PaddingTop(15);

                column.Item().AlignRight().Text(text =>
                {
                    text.Span("Total Books: ").Bold();

                    text.Span(
                        borrow.BorrowDetails.Sum(x => x.Quantity).ToString());
                });

                column.Item().PaddingTop(25);

                column.Item().AlignCenter().Text(
                    "Please return books before the due date.")
                    .Italic();

                column.Item().AlignCenter().Text(
                    "Thank you for using our library.")
                    .FontSize(12)
                    .Bold();
            });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated at ");

                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                            .SemiBold();
                    });

            });

            })
.GeneratePdf();
        }
    }
}