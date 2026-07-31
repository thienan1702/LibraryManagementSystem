using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class OverdueReportPdf : IDocument
{
    private readonly List<Borrow> _borrows;

    public OverdueReportPdf(List<Borrow> borrows)
    {
        _borrows = borrows
            .Where(x => !x.IsReturned &&
                        x.DueDate.Date < DateTime.Today)
            .ToList();
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
                x.FontSize(10)
            );

            //========================
            // HEADER
            //========================

            page.Header().Column(column =>
            {
                ComposeHeader(column);
            });

            //========================
            // CONTENT
            //========================

            page.Content().Layers(layers =>
            {
                layers.Layer()
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("LIBRARY")
                    .FontSize(130)
                    .FontColor("#EFEFEF")
                    .Bold();

                layers.PrimaryLayer()
                    .Column(column =>
                    {
                        column.Spacing(20);

                        ComposeSummary(column);

                        ComposeTable(column);

                        ComposeFooterSummary(column);

                        ComposeSignature(column);
                    });
            });

            //========================
            // FOOTER
            //========================

            //================ FOOTER =================

            page.Footer()
                .PaddingTop(10)
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Row(row =>
                {
                    row.RelativeItem()
                        .AlignLeft()
                        .Text(text =>
                        {
                            text.Span("Library Management • Fine Report")
                                .SemiBold()
                                .FontSize(9)
                                .FontColor(Colors.Blue.Darken2);
                        });


                    row.RelativeItem()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span(
                                $"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}"
                            )
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken1);
                        });

                    row.RelativeItem()
                        .AlignRight()
                        .Text(text =>
                        {
                            text.Span("Page ")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            text.CurrentPageNumber()
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            text.Span(" / ")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);

                            text.TotalPages()
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);
                        });
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
            // Logo
            //row.ConstantItem(70)
            //    .Height(70)
            //    .Image("wwwroot/images/logo.png");

            // Tiêu đề
            row.RelativeItem().Column(col =>
            {
                col.Item()
                   .Text("OVERDUE REPORT")
                    .FontColor(Colors.Red.Darken2)
                    .Bold()
                    .FontSize(24);

                col.Item()
                    .Text("Borrow Report")
                    .SemiBold()
                    .FontSize(17);

                col.Item()
                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Darken2);
            });
        });

        column.Item()
            .PaddingTop(8)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);
    }

    //======================================================
    // PART 2
    //======================================================

    private void ComposeSummary(ColumnDescriptor column)
    {
        int totalOverdue = _borrows.Count;

        int totalLateDays =
            _borrows.Sum(x => x.OverdueDays);

        decimal totalFine =
            _borrows.Sum(x => x.FineAmount);

        double averageLate =
            totalOverdue == 0
                ? 0
                : (double)totalLateDays / totalOverdue;

        column.Item().Row(row =>
        {
            row.Spacing(10);

            row.RelativeItem().Element(c =>
                SummaryCard(
                    c,
                    "Overdue Books",
                    totalOverdue.ToString(),
                    Colors.Red.Medium));

            row.RelativeItem().Element(c =>
                SummaryCard(
                    c,
                    "Late Days",
                    totalLateDays.ToString(),
                    Colors.Orange.Medium));

            row.RelativeItem().Element(c =>
                SummaryCard(
                    c,
                    "Average",
                    $"{averageLate:F1}",
                    Colors.Blue.Medium));

            row.RelativeItem().Element(c =>
                SummaryCard(
                    c,
                    "Total Fine",
                    $"{totalFine:N0} đ",
                    Colors.Green.Medium));
        });

        column.Item().PaddingBottom(15);
    }

    //======================================================
    // PART 3
    //======================================================

    private void ComposeTable(ColumnDescriptor column)
    {
        column.Item()
            .Text("OVERDUE BOOK LIST")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Red.Darken2);

        column.Item().PaddingTop(8);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(35);      // No
                columns.RelativeColumn(2);       // Borrower
                columns.RelativeColumn(2);       // Book
                columns.RelativeColumn();        // Due Date
                columns.RelativeColumn();        // Late Days
                columns.RelativeColumn();        // Fine
            });

            //---------------- HEADER ----------------

            HeaderCell(table.Cell(), "No.");
            HeaderCell(table.Cell(), "Borrower");
            HeaderCell(table.Cell(), "Book");
            HeaderCell(table.Cell(), "Due Date");
            HeaderCell(table.Cell(), "Late Days");
            HeaderCell(table.Cell(), "Fine");

            //---------------- DATA ----------------

            int no = 1;

            foreach (var item in _borrows)
            {
                int lateDays =
                    (DateTime.Today - item.DueDate.Date).Days;

                string rowColor =
                    no % 2 == 0
                        ? Colors.Grey.Lighten5
                        : Colors.White;

                BodyCell(table.Cell(), no.ToString(), rowColor, true);

                BodyCell(table.Cell(), item.BorrowerName, rowColor);

                BodyCell(
                     table.Cell(),
                     string.Join(", ",
                         item.BorrowDetails
                             .Where(x => x.Book != null)
                             .Select(x => x.Book!.Title)),
                     rowColor);
                BodyCell(
                    table.Cell(),
                    item.DueDate.ToString("dd/MM/yyyy"),
                    rowColor,
                    true);

                LateDayCell(
                    table.Cell(),
                    lateDays,
                    rowColor);

                FineCell(
                    table.Cell(),
                    item.FineAmount,
                    rowColor);

                no++;
            }
        });
    }

    //======================================================
    // PART 4
    //======================================================
    private void ComposeFooterSummary(ColumnDescriptor column)
    {
        int totalOverdue = _borrows.Count;

        int totalLateDays =
            _borrows.Sum(x =>
                (DateTime.Today - x.DueDate.Date).Days);

        decimal totalFine =
            _borrows.Sum(x => x.FineAmount);

        double averageLate =
            totalOverdue == 0
                ? 0
                : (double)totalLateDays / totalOverdue;

        column.Item()
            .PaddingTop(12)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(10);

        column.Item()
            .Text("REPORT SUMMARY")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Red.Darken2);

        column.Item().PaddingTop(8);

        column.Item()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Overdue Books : {totalOverdue}");

                    col.Item().PaddingTop(5);

                    col.Item().Text($"Total Late Days : {totalLateDays}");

                    col.Item().PaddingTop(5);

                    col.Item().Text($"Average Late : {averageLate:F1} days");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text($"Total Fine : {totalFine:N0} đ")
                        .Bold()
                        .FontColor(Colors.Red.Darken2);

                    col.Item().PaddingTop(5);

                    col.Item()
                        .Text($"Generated : {DateTime.Now:dd/MM/yyyy}");

                    col.Item().PaddingTop(5);

                    col.Item()
                        .Text("Status : Completed")
                        .FontColor(Colors.Green.Darken2);
                });
            });
    }


    //======================================================
    // PART 5
    //======================================================

    private void ComposeSignature(ColumnDescriptor column)
    {
        column.Item().PaddingTop(20);

        column.Item()
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(15);

        column.Item()
            .AlignRight()
            .Text($"Date : {DateTime.Now:dd/MM/yyyy}")
            .FontSize(10)
            .Italic();

        column.Item().PaddingTop(15);

        column.Item().Row(row =>
        {
            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("Prepared By")
                        .Bold();

                    col.Item()
                        .PaddingTop(60);

                    col.Item()
                        .LineHorizontal(1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Administrator)")
                        .FontSize(9);
                });

            row.ConstantItem(100);

            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("Approved By")
                        .Bold();

                    col.Item()
                        .PaddingTop(60);

                    col.Item()
                        .LineHorizontal(1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Library Manager)")
                        .FontSize(9);
                });
        });
    }
    private void HeaderCell(IContainer cell, string text)
    {
        cell
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Red.Darken2)
            .Padding(7)
            .AlignCenter()
            .AlignMiddle()
            .Text(text)
            .Bold()
            .FontColor(Colors.White);
    }

    private void BodyCell(
     IContainer cell,
     string text,
     string background,
     bool center = false)
    {
        var content = cell
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(background)
            .Padding(6);

        if (center)
        {
            content
                .AlignCenter()
                .AlignMiddle()
                .Text(text)
                .FontSize(10);
        }
        else
        {
            content
                .AlignLeft()
                .AlignMiddle()
                .Text(text)
                .FontSize(10);
        }
    }
    private void LateDayCell(
    IContainer cell,
    int lateDays,
    string background)
    {
        cell
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(background)
            .Padding(5)
            .AlignCenter()
            .AlignMiddle()
            .Text($"{lateDays} days")
            .Bold()
            .FontColor(Colors.Red.Darken2);
    }
    private void FineCell(
    IContainer cell,
    decimal fine,
    string background)
    {
        cell
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(background)
            .Padding(5)
            .AlignRight()
            .AlignMiddle()
            .Text($"{fine:N0} đ")
            .Bold()
            .FontColor(Colors.Red.Darken2);
    }

    private static void SummaryCard(
    IContainer container,
    string title,
    string value,
    string color)
    {
        container
            .Padding(8)
            .Background(color)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .CornerRadius(6)
            .Column(col =>
            {
                col.Item()
                    .AlignCenter()
                    .Text(title)
                    .FontSize(10)
                    .FontColor(Colors.White);

                col.Item()
                    .PaddingTop(5);

                col.Item()
                    .AlignCenter()
                    .Text(value)
                    .Bold()
                    .FontSize(18)
                    .FontColor(Colors.White);
            });
    }

}