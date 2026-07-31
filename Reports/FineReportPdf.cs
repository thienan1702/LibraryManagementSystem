using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class FineReportPdf : IDocument
{
    private readonly List<Borrow> _borrows;

    public FineReportPdf(List<Borrow> borrows)
    {
        _borrows = borrows;
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
                    .Text("BOOK FINE REPORT")
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
        int totalBorrow = _borrows.Count;

        int totalLate =
            _borrows.Count(x => x.FineAmount > 0);

        decimal totalFine =
            _borrows.Sum(x => x.FineAmount);

        decimal averageFine =
            totalLate == 0
                ? 0
                : totalFine / totalLate;

        column.Item().Row(row =>
        {
            row.Spacing(10);

            SummaryCard(
                row.RelativeItem(),
                "Total Fine",
                $"{totalFine:N0} đ",
                Colors.Red.Medium);

            SummaryCard(
                row.RelativeItem(),
                "Late Borrow",
                totalLate.ToString(),
                Colors.Orange.Medium);

            SummaryCard(
                row.RelativeItem(),
                "Average Fine",
                $"{averageFine:N0} đ",
                Colors.Blue.Medium);

            SummaryCard(
                row.RelativeItem(),
                "Borrow Records",
                totalBorrow.ToString(),
                Colors.Green.Medium);
        });

        column.Item().PaddingBottom(15);
    }

    //======================================================
    // PART 3
    //======================================================

    private void ComposeTable(ColumnDescriptor column)
    {
        column.Item().Text("BORROW DETAILS")
        .Bold()
        .FontSize(13)
        .FontColor(Colors.Blue.Darken2);


        column.Item().PaddingTop(8);

        column.Item().Table(table =>
        {
            //==================================================
            // COLUMN WIDTH
            //==================================================

            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(35);     // No.
                columns.RelativeColumn(1.5f);   // Borrower
                columns.RelativeColumn(2f);     // Email
                columns.RelativeColumn(1f);     // Borrow Date
                columns.RelativeColumn(1f);     // Due Date
                columns.RelativeColumn(1f);     // Status
                columns.RelativeColumn(1f);     // Fine
            });

            //==================================================
            // TABLE HEADER
            //==================================================

            table.Header(header =>
            {
                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("No.")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Borrower")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Email")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Borrow Date")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Due Date")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Late Days")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background("#1E3A8A")
                    .Border(1)
                    .BorderColor(Colors.White)
                    .PaddingVertical(10)
                    .PaddingHorizontal(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Fine")
                    .Bold()
                    .FontColor(Colors.White);
            });

            //==================================================
            // EMPTY DATA
            //==================================================

            if (_borrows == null || !_borrows.Any())
            {
                table.Cell()
                    .ColumnSpan(7)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(15)
                    .AlignCenter()
                    .Text("No borrowing records found.")
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);

                return;
            }

            //==================================================
            // TABLE DATA
            //==================================================

            int number = 1;

            foreach (var item in _borrows)
            {
                bool isOverdue =
                    !item.IsReturned &&
                    item.DueDate.Date < DateTime.Today;

                int lateDays = 0;

                if (item.IsReturned)
                {
                    lateDays = Math.Max(
                        0,
                        (item.ReturnDate?.Date - item.DueDate.Date)?.Days ?? 0);
                }
                else
                {
                    lateDays = Math.Max(
                        0,
                        (DateTime.Today - item.DueDate.Date).Days);
                }

                string backgroundColor;

                if (isOverdue)
                {
                    backgroundColor = Colors.Red.Lighten5;
                }
                else
                {
                    backgroundColor =
                        number % 2 == 0
                            ? Colors.Grey.Lighten5
                            : Colors.White;
                }

                string lateColor;

                if (lateDays == 0)
                {
                    lateColor = Colors.Green.Darken2;
                }
                else if (lateDays <= 7)
                {
                    lateColor = Colors.Orange.Darken2;
                }
                else
                {
                    lateColor = Colors.Red.Darken2;
                }

                // No.

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(number.ToString());

                // Borrower

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignMiddle()
                    .Text(item.BorrowerName)
                    .SemiBold();

                // Email

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignMiddle()
                    .Text(item.BorrowerEmail)
                    .FontSize(9);

                // Borrow Date

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(item.BorrowDate.ToString("dd/MM/yyyy"));

                // Due Date

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(item.DueDate.ToString("dd/MM/yyyy"));

                // Late day

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(lateDays.ToString())
                    .Bold()
                    .FontColor(lateColor);

                // Fine

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignRight()
                    .AlignMiddle()
                    .Text($"{item.FineAmount:N0} đ")
                    .Bold()
                    .FontColor(
                        item.FineAmount > 0
                            ? Colors.Red.Darken2
                            : Colors.Grey.Darken1
                    );

                number++;
            }
        });

    }


    //======================================================
    // PART 4
    //======================================================
    private void ComposeFooterSummary(ColumnDescriptor column)
    {
        int totalBorrow = _borrows.Count;


        int returned = _borrows.Count(x => x.IsReturned);

        int borrowing = _borrows.Count(x => !x.IsReturned);

        int overdue = _borrows.Count(x =>
            !x.IsReturned &&
            x.DueDate.Date < DateTime.Today);

        decimal totalFine = _borrows.Sum(x => x.FineAmount);

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

        column.Item().PaddingTop(7);

        // Nội dung tổng kết
        column.Item()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5)
            .Padding(12)
            .Row(row =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.ConstantColumn(120);
                    });

                    void Add(string left, string right)
                    {
                        table.Cell().Padding(5).Text(left).Bold();

                        table.Cell().Padding(5).AlignRight().Text(right);
                    }

                    Add("Total Borrow", totalBorrow.ToString());

                    Add("Returned", returned.ToString());

                    Add("Borrowing", borrowing.ToString());

                    Add("Overdue", overdue.ToString());

                    Add("Total Fine", $"{totalFine:N0} đ");
                });
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