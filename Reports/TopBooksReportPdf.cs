using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class TopBooksReportPdf : IDocument
{
    private readonly List<TopBookViewModel> _books;

    public TopBooksReportPdf(List<TopBookViewModel> books)
    {
        _books = books;
    }
    public DocumentMetadata GetMetadata()
        => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(25);

            page.Size(PageSizes.A4);

            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(column =>
            {
                ComposeHeader(column);
            });

            page.Content().Column(column =>
            {
                ComposeSummary(column);

                ComposeTop3(column);

                column.Item().PaddingTop(15);

                ComposeTable(column);

            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generated: ");

                x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

                x.Span("    |    ");

                x.CurrentPageNumber();

                x.Span(" / ");

                x.TotalPages();
            });
        });
    }
    //======================================================
    // HEADER
    //======================================================

    private void ComposeHeader(ColumnDescriptor column)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(70)
                .AlignCenter()
                .AlignMiddle()
                .Text("📚")
                .FontSize(34);

            row.RelativeItem()
                .Column(c =>
                {
                    c.Item().Text("LIBRARY MANAGEMENT")
                        .Bold()
                        .FontSize(20);

                    c.Item().Text("Top Borrowed Books Report")
                        .FontSize(15);

                    c.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                });
        });

        column.Item()
            .PaddingTop(10)
            .LineHorizontal(1)
            .LineColor(Colors.Blue.Medium);
    }
    //======================================================
    // PART 2
    //======================================================

    private void ComposeSummary(ColumnDescriptor column)
    {
        int totalBooks = _books.Count;

        int totalBorrow = _books.Sum(x => x.BorrowCount);

        string topBook = _books.Any()
            ? _books.OrderByDescending(x => x.BorrowCount)
                    .First().BookTitle
            : "-";

        int highestBorrow = _books.Any()
            ? _books.Max(x => x.BorrowCount)
            : 0;

        column.Item().Row(row =>
        {
            SummaryCard(
                row.RelativeItem(),
                "📚 Books",
                totalBooks.ToString(),
                Colors.Blue.Lighten3);

            SummaryCard(
                row.RelativeItem(),
                "🔥 Borrow",
                totalBorrow.ToString(),
                Colors.Green.Lighten3);

            SummaryCard(
                row.RelativeItem(),
                "🥇 Top Book",
                topBook,
                Colors.Orange.Lighten3);

            SummaryCard(
                row.RelativeItem(),
                "⭐ Highest",
                highestBorrow.ToString(),
                Colors.Red.Lighten3);
        });
    }

    private void ComposeTop3(ColumnDescriptor column)
    {
        var top3 = _books
            .OrderByDescending(x => x.BorrowCount)
            .Take(3)
            .ToList();

        column.Item().PaddingTop(15);

        column.Item()
            .Text("TOP 3 BOOKS")
            .Bold()
            .FontSize(14);

        column.Item().PaddingTop(8);

        foreach (var item in top3)
        {
            string medal =
                top3.IndexOf(item) switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    _ => "🥉"
                };

            column.Item()
                .Background(Colors.Grey.Lighten4)
                .Padding(8)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Row(r =>
                {
                    r.ConstantItem(40)
                        .AlignMiddle()
                        .Text(medal)
                        .FontSize(20);

                    r.RelativeItem()
                        .Text(item.BookTitle)
                        .Bold();

                    r.ConstantItem(70)
                        .AlignRight()
                        .Text(item.BorrowCount.ToString())
                        .Bold();
                });

            column.Item().PaddingBottom(4);
        }
    }


    //======================================================
    // PART 3
    //======================================================

    private void ComposeTable(ColumnDescriptor column)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);

                columns.RelativeColumn(4);

                columns.RelativeColumn(3);

                columns.RelativeColumn(2);

                columns.RelativeColumn(2);
            });

            HeaderCell(table.Cell(), "Rank");
            HeaderCell(table.Cell(), "Book");
            HeaderCell(table.Cell(), "Author");
            HeaderCell(table.Cell(), "Category");
            HeaderCell(table.Cell(), "Borrow");

            int rank = 1;

            foreach (var item in _books)
            {
                string color =
                    rank % 2 == 0
                    ? Colors.Grey.Lighten5
                    : Colors.White;

                string rankText = rank switch
                {
                    1 => "🥇",
                    2 => "🥈",
                    3 => "🥉",
                    _ => rank.ToString()
                };

                BodyCell(table.Cell(), rankText, color, true); BodyCell(table.Cell(), item.BookTitle, color);
                BodyCell(table.Cell(), item.Author, color);
                BodyCell(table.Cell(), item.Category, color);
                BorrowProgressCell(
                    table.Cell(),
                    item.BorrowCount,
                    _books.Max(x => x.BorrowCount),
                    color);
                rank++;
            }
        });
    }

    private void BorrowProgressCell(
    IContainer container,
    int borrowCount,
    int maxBorrow,
    string background)
    {
        float percent = maxBorrow == 0
            ? 0
            : (float)borrowCount / maxBorrow;

        container
            .Background(background)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5)
            .Column(col =>
            {
                col.Item().Text($"{borrowCount}")
                    .Bold()
                    .FontSize(10);

                col.Item()
                    .Height(8)
                    .Background(Colors.Grey.Lighten2)
                    .Row(row =>
                    {
                        row.RelativeItem(percent)
                            .Background(Colors.Green.Medium);

                        if (percent < 1)
                            row.RelativeItem(1 - percent);
                    });
            });
    }

    private void SummaryCard(
    IContainer container,
    string title,
    string value,
    string color)
    {
        container
            .Padding(5)
            .Background(color)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Column(c =>
            {
                c.Item()
                    .Text(title)
                    .FontSize(11);

                c.Item()
                    .Text(value)
                    .Bold()
                    .FontSize(18);
            });
    }

    private void HeaderCell(IContainer cell, string text)
    {
        cell
            .Background(Colors.Blue.Darken2)
            .Padding(6)
            .Border(1)
            .BorderColor(Colors.White)
            .AlignCenter()
            .Text(text)
            .FontColor(Colors.White)
            .Bold();
    }

    private void BodyCell(
    IContainer cell,
    string text,
    string background,
    bool center = false)
    {
        var container = cell
            .Background(background)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(5);

        if (center)
            container = container.AlignCenter();

        container.Text(text);
    }
    //======================================================
    // PART 4
    //======================================================
    private void ComposeFooterSummary(ColumnDescriptor column)
    {
        int totalBooks = _books.Count;

        int totalBorrow = _books.Sum(x => x.BorrowCount);

        string topBook = _books.Any()
            ? _books.OrderByDescending(x => x.BorrowCount)
                    .First().BookTitle
            : "-";

        int highestBorrow = _books.Any()
            ? _books.Max(x => x.BorrowCount)
            : 0;

        // Đường phân cách
        column.Item()
            .PaddingTop(10)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(10);

        // Tiêu đề
        column.Item()
            .Text("REPORT SUMMARY")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Blue.Darken2);

        column.Item().PaddingTop(8);

        // Bảng tổng kết
        column.Item()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(170);
                });

                void Add(string left, string right)
                {
                    table.Cell()
                        .PaddingVertical(4)
                        .Text(left)
                        .Bold();

                    table.Cell()
                        .PaddingVertical(4)
                        .AlignRight()
                        .Text(right);
                }

                Add("Total Books", totalBooks.ToString());

                Add("Total Borrow", totalBorrow.ToString());

                Add("Most Borrowed Book", topBook);

                Add("Highest Borrow Count", highestBorrow.ToString());

                Add("Generated At", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            });
    }


    //======================================================
    // PART 5
    //======================================================

    private void ComposeSignature(ColumnDescriptor column)
    {
        column.Item().PaddingTop(20);


// Đường phân cách
column.Item()
    .LineHorizontal(1)
    .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(15);

        // Ngày lập báo cáo
        column.Item()
            .AlignRight()
            .Text($"Date: {DateTime.Now:dd/MM/yyyy}")
            .FontSize(10)
            .Italic()
            .FontColor(Colors.Grey.Darken1);

        column.Item().PaddingTop(10);

        // Khu vực ký tên
        column.Item().Row(row =>
        {
            //=========================================
            // PREPARED BY
            //=========================================

            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("PREPARED BY")
                        .Bold()
                        .FontSize(11)
                        .FontColor(Colors.Blue.Darken2);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Administrator)")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    // Khoảng trống để ký
                    col.Item()
                        .Height(55);

                    col.Item()
                        .AlignCenter()
                        .Width(140)
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Darken1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Full name and signature)")
                        .FontSize(8)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });

            // Khoảng cách giữa hai bên
            row.ConstantItem(80);

            //=========================================
            // APPROVED BY
            //=========================================

            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("APPROVED BY")
                        .Bold()
                        .FontSize(11)
                        .FontColor(Colors.Green.Darken2);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Library Manager)")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    // Khoảng trống để ký
                    col.Item()
                        .Height(55);

                    col.Item()
                        .AlignCenter()
                        .Width(140)
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Darken1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Full name and signature)")
                        .FontSize(8)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                });
        });


}

}