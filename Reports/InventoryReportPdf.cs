using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class InventoryReportPdf : IDocument
{
    private readonly List<Book> _books;

    public InventoryReportPdf(List<Book> books)
    {
        _books = books;
    }

    public DocumentMetadata GetMetadata()
        => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());

            page.Margin(25);

            page.DefaultTextStyle(x =>
                x.FontSize(10));

            //==========================
            // HEADER
            //==========================

            page.Header().Column(column =>
            {
                ComposeHeader(column);
            });

            //==========================
            // CONTENT
            //==========================

            page.Content().Column(column =>
            {
                column.Spacing(20);

                ComposeSummary(column);

                ComposeTable(column);

                ComposeFooterSummary(column);

                ComposeSignature(column);
            });

            //==========================
            // FOOTER
            //==========================

            page.Footer()
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingTop(8)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text("Library Management System")
                        .FontSize(9);

                    row.RelativeItem()
                        .AlignCenter()
                        .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(8);

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Page ");

                            text.CurrentPageNumber();

                            text.Span(" / ");

                            text.TotalPages();
                        });
                });
        });
    }

    private void ComposeHeader(ColumnDescriptor column)
    {
        column.Spacing(8);

        column.Item()
            .AlignCenter()
            .Text("LIBRARY MANAGEMENT SYSTEM")
            .Bold()
            .FontSize(24)
            .FontColor(Colors.Blue.Darken2);

        column.Item()
            .AlignCenter()
            .Text("INVENTORY REPORT")
            .Bold()
            .FontSize(17);

        column.Item()
            .AlignCenter()
            .Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}")
            .FontColor(Colors.Grey.Darken2);

        column.Item()
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);
 
    
    }


    private void ComposeSummary(ColumnDescriptor column)
    {
        int totalBooks = _books.Count;

        int totalCategory =
            _books
            .Select(x => x.CategoryId)
            .Distinct()
            .Count();

        int totalAuthor =
            _books
            .Select(x => x.AuthorId)
            .Distinct()
            .Count();

        int lowStock =
            _books.Count(x => x.AvailableQuantity <= 5 && x.AvailableQuantity > 0);

        int outStock =
            _books.Count(x => x.AvailableQuantity == 0);

        column.Item().Row(row =>
        {
            row.Spacing(10);

            SummaryCard(row.RelativeItem(),
                "TOTAL BOOKS",
                totalBooks.ToString(),
                Colors.Blue.Darken2);

            SummaryCard(row.RelativeItem(),
                "CATEGORIES",
                totalCategory.ToString(),
                Colors.Green.Darken2);

            SummaryCard(row.RelativeItem(),
                "AUTHORS",
                totalAuthor.ToString(),
                Colors.Orange.Darken2);

            SummaryCard(row.RelativeItem(),
                "LOW STOCK",
                lowStock.ToString(),
                Colors.Red.Darken2);

            SummaryCard(row.RelativeItem(),
                "OUT OF STOCK",
                outStock.ToString(),
                Colors.Purple.Darken2);
        });
    }


    private void SummaryCard(
    IContainer container,
    string title,
    string value,
    string color)
    {
        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(10)
            .Column(col =>
            {
                col.Item()
                    .Text(title)
                    .Bold()
                    .FontSize(10)
                    .FontColor(color);

                col.Item()
                    .PaddingTop(6);

                col.Item()
                    .Text(value)
                    .FontSize(22)
                    .Bold();
            });
    }
    private void ComposeTable(ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(10)
            .Text("BOOK INVENTORY")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Blue.Darken2);

        column.Item().PaddingTop(8);

        column.Item().Table(table =>
        {
            //==========================
            // Columns
            //==========================

            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);      // No
                columns.RelativeColumn(3);       // Title
                columns.RelativeColumn(2);       // Author
                columns.RelativeColumn(2);       // Category
                columns.ConstantColumn(70);      // Total
                columns.ConstantColumn(80);      // Available
                columns.ConstantColumn(90);      // Status
            });

            //==========================
            // Header
            //==========================

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "No");
                HeaderCell(header.Cell(), "Book Title");
                HeaderCell(header.Cell(), "Author");
                HeaderCell(header.Cell(), "Category");
                HeaderCell(header.Cell(), "Total");
                HeaderCell(header.Cell(), "Available");
                HeaderCell(header.Cell(), "Status");
            });

            //==========================
            // Data
            //==========================

            int no = 1;

            foreach (var item in _books)
            {
                string status;
                string rowColor;

                if (item.AvailableQuantity == 0)
                {
                    status = "Out of Stock";
                    rowColor = Colors.Red.Lighten5;
                }
                else if (item.AvailableQuantity <= 5)
                {
                    status = "Low Stock";
                    rowColor = Colors.Orange.Lighten5;
                }
                else
                {
                    status = "Available";
                    rowColor = no % 2 == 0
                        ? Colors.Grey.Lighten5
                        : Colors.White;
                }

                BodyCell(table.Cell(), no.ToString(), rowColor);

                BodyCell(
                    table.Cell(),
                    item.Title,
                    rowColor);
                
                BodyCell(
                    table.Cell(),
                    item.Author?.Name ?? "",
                    rowColor);

                BodyCell(table.Cell(),
                    item.Category?.Name ?? "",
                    rowColor);

                BodyCell(table.Cell(),
                    item.Quantity.ToString(),
                    rowColor,
                    true);

                InventoryProgressCell(
                     table.Cell(),
                     item.Quantity,
                     item.AvailableQuantity,
                     rowColor);

                StatusCell(
                    table.Cell(),
                    status,
                    rowColor);

                no++;
            }
        });
    }
    private void HeaderCell(
      IContainer container,
      string text)
    {
        container
            .Border(1)
            .BorderColor(Colors.White)
            .Background(Colors.Blue.Darken2)
            .Padding(6)
            .AlignCenter()
            .AlignMiddle()
            .Text(text)
            .Bold()
            .FontColor(Colors.White);
    }
    private void BodyCell(
      IContainer container,
      string text,
      string background,
      bool center = false)
    {
        if (center)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(background)
                .Padding(5)
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontSize(10);
        }
        else
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(background)
                .Padding(5)
                .AlignMiddle()
                .Text(text)
                .FontSize(10);
        }
    }

    private void StatusCell(
     IContainer container,
     string status,
     string background)
    {
        string color = Colors.Green.Darken2;

        if (status == "Low Stock")
            color = Colors.Orange.Darken2;

        if (status == "Out of Stock")
            color = Colors.Red.Darken2;

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(background)
            .Padding(5)
            .AlignCenter()
            .AlignMiddle()
            .Text(status)
            .Bold()
            .FontColor(color);
    }

    private void InventoryProgressCell(
    IContainer container,
    int total,
    int available,
    string background)
    {
        float percent = total == 0
            ? 0
            : (float)available / total;

        string color = Colors.Green.Medium;

        if (percent <= 0.2f)
            color = Colors.Red.Medium;
        else if (percent <= 0.5f)
            color = Colors.Orange.Medium;

        int barWidth = 70; // chiều rộng progress bar

        int filledWidth = (int)(barWidth * percent);

        if (filledWidth < 0)
            filledWidth = 0;

        if (filledWidth > barWidth)
            filledWidth = barWidth;

        int emptyWidth = barWidth - filledWidth;

        container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(background)
            .Padding(5)
            .Column(col =>
            {
                col.Item()
                    .AlignCenter()
                    .Text($"{available}/{total}")
                    .FontSize(9);

                col.Item()
                    .PaddingTop(3);

                col.Item()
                    .Row(row =>
                    {
                        if (filledWidth > 0)
                        {
                            row.ConstantItem(filledWidth)
                                .Height(6)
                                .Background(color);
                        }

                        if (emptyWidth > 0)
                        {
                            row.ConstantItem(emptyWidth)
                                .Height(6)
                                .Background(Colors.Grey.Lighten2);
                        }
                    });
            });
    }
    private void ComposeFooterSummary(ColumnDescriptor column)
    {
        int totalBooks = _books.Count;

        int totalCopies =
            _books.Sum(x => x.Quantity);

        int available =
            _books.Sum(x => x.AvailableQuantity);

        int borrowed =
            totalCopies - available;

        int lowStock =
            _books.Count(x =>
                x.AvailableQuantity <= 5 &&
                x.AvailableQuantity > 0);

        int outStock =
            _books.Count(x =>
                x.AvailableQuantity == 0);

        column.Item()
            .PaddingTop(15);

        column.Item()
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item()
            .PaddingTop(10);

        column.Item()
            .Text("INVENTORY SUMMARY")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Blue.Darken2);

        column.Item()
            .PaddingTop(8);

        column.Item()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total Books : {totalBooks}");

                    col.Item().PaddingTop(5);

                    col.Item().Text($"Total Copies : {totalCopies}");

                    col.Item().PaddingTop(5);

                    col.Item().Text($"Available : {available}");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Borrowed : {borrowed}");

                    col.Item().PaddingTop(5);

                    col.Item()
                        .Text($"Low Stock : {lowStock}")
                        .FontColor(
                            lowStock > 0
                                ? Colors.Orange.Darken2
                                : Colors.Grey.Darken2);

                    col.Item().PaddingTop(5);

                    col.Item()
                        .Text($"Out Of Stock : {outStock}")
                        .FontColor(
                            outStock > 0
                                ? Colors.Red.Darken2
                                : Colors.Grey.Darken2);
                });
            });
    }
    private void ComposeSignature(ColumnDescriptor column)
    {
        column.Item()
            .PaddingTop(25);

        column.Item()
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item()
            .PaddingTop(10);

        column.Item()
            .AlignRight()
            .Text($"Report Date : {DateTime.Now:dd/MM/yyyy}")
            .FontSize(10)
            .Italic();

        column.Item()
            .PaddingTop(15);

        column.Item()
            .Row(row =>
            {
                row.RelativeItem()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item()
                            .Text("Prepared By")
                            .Bold()
                            .FontSize(11);

                        col.Item()
                            .Text("(Administrator)")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item()
                            .Height(60);

                        col.Item()
                            .Width(150)
                            .AlignCenter()
                            .LineHorizontal(1);

                        col.Item()
                            .PaddingTop(5)
                            .Text("(Signature)")
                            .Italic()
                            .FontSize(8);
                    });

                row.ConstantItem(80);

                row.RelativeItem()
                    .AlignCenter()
                    .Column(col =>
                    {
                        col.Item()
                            .Text("Approved By")
                            .Bold()
                            .FontSize(11);

                        col.Item()
                            .Text("(Library Manager)")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item()
                            .Height(60);

                        col.Item()
                            .Width(150)
                            .AlignCenter()
                            .LineHorizontal(1);

                        col.Item()
                            .PaddingTop(5)
                            .Text("(Signature)")
                            .Italic()
                            .FontSize(8);
                    });
            });
    }
}