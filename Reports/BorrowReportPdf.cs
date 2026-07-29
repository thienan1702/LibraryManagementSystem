using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class BorrowReportPdf : IDocument
{
    private readonly List<Borrow> _borrows;

    public BorrowReportPdf(List<Borrow> borrows)
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

            page.Content().Column(column =>
            {
                column.Spacing(20);

                ComposeSummary(column);

                // PART 2
                 ComposeTable(column);

                // PART 3
                ComposeFooterSummary(column);

                // PART 4
                 ComposeSignature(column);
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
                            text.Span("Library Management System")
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
        column.Spacing(10);

        column.Item()
            .AlignCenter()
            .Text("LIBRARY MANAGEMENT SYSTEM")
            .Bold()
            .FontSize(24)
            .FontColor(Colors.Blue.Darken2);

        column.Item()
            .AlignCenter()
            .Text("BORROW REPORT")
            .Bold()
            .FontSize(17);

        column.Item()
            .AlignCenter()
            .Text($"Generated on {DateTime.Now:dd/MM/yyyy HH:mm}")
            .FontColor(Colors.Grey.Darken2);

        column.Item()
            .PaddingTop(5)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);
    }

    //======================================================
    // PART 2
    //======================================================

    private void ComposeSummary(ColumnDescriptor column)
    {
        int totalBorrow = _borrows.Count;

        int returned = _borrows.Count(x => x.IsReturned);

        int borrowing = _borrows.Count(x => !x.IsReturned);

        int overdue = _borrows.Count(x =>
            !x.IsReturned &&
            x.DueDate.Date < DateTime.Today);

        decimal totalFine = _borrows.Sum(x => x.FineAmount);

        column.Item().Row(row =>
        {
            row.Spacing(10);

            row.RelativeItem().Border(1)
                .BorderColor(Colors.Blue.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text("TOTAL BORROW")
                        .Bold()
                        .FontSize(10)
                        .FontColor(Colors.Blue.Darken2);

                    c.Item().Text(totalBorrow.ToString())
                        .FontSize(22)
                        .Bold();
                });

            row.RelativeItem().Border(1)
                .BorderColor(Colors.Green.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text("RETURNED")
                        .Bold()
                        .FontSize(10)
                        .FontColor(Colors.Green.Darken2);

                    c.Item().Text(returned.ToString())
                        .FontSize(22)
                        .Bold();
                });

            row.RelativeItem().Border(1)
                .BorderColor(Colors.Orange.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text("BORROWING")
                        .Bold()
                        .FontSize(10)
                        .FontColor(Colors.Orange.Darken2);

                    c.Item().Text(borrowing.ToString())
                        .FontSize(22)
                        .Bold();
                });

            row.RelativeItem().Border(1)
                .BorderColor(Colors.Red.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text("OVERDUE")
                        .Bold()
                        .FontSize(10)
                        .FontColor(Colors.Red.Darken2);

                    c.Item().Text(overdue.ToString())
                        .FontSize(22)
                        .Bold();
                });

            row.RelativeItem().Border(1)
                .BorderColor(Colors.Purple.Lighten2)
                .Padding(10)
                .Column(c =>
                {
                    c.Item().Text("TOTAL FINE")
                        .Bold()
                        .FontSize(10)
                        .FontColor(Colors.Purple.Darken2);

                    c.Item().Text($"{totalFine:N0} đ")
                        .FontSize(18)
                        .Bold();
                });
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
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("No.")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Borrower")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Email")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Borrow Date")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Due Date")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Status")
                    .Bold()
                    .FontColor(Colors.White);

                header.Cell()
                    .Background(Colors.Blue.Darken2)
                    .Border(1)
                    .BorderColor(Colors.White)
                    .Padding(7)
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

                string status;

                if (item.IsReturned)
                {
                    status = "Returned";
                }
                else if (isOverdue)
                {
                    status = "Overdue";
                }
                else
                {
                    status = "Borrowing";
                }

                string backgroundColor =
                    number % 2 == 0
                        ? Colors.Grey.Lighten5
                        : Colors.White;

                string statusColor;

                if (status == "Returned")
                {
                    statusColor = Colors.Green.Darken2;
                }
                else if (status == "Overdue")
                {
                    statusColor = Colors.Red.Darken2;
                }
                else
                {
                    statusColor = Colors.Orange.Darken2;
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

                // Status

                table.Cell()
                    .Background(backgroundColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(status)
                    .Bold()
                    .FontColor(statusColor);

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
                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text($"Total Borrow: {totalBorrow}")
                        .FontSize(10);

                    col.Item()
                        .PaddingTop(5)
                        .Text($"Returned: {returned}")
                        .FontSize(10);

                    col.Item()
                        .PaddingTop(5)
                        .Text($"Borrowing: {borrowing}")
                        .FontSize(10);
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text($"Overdue: {overdue}")
                        .FontSize(10)
                        .FontColor(
                            overdue > 0
                                ? Colors.Red.Darken2
                                : Colors.Grey.Darken2
                        );

                    col.Item()
                        .PaddingTop(5)
                        .Text($"Total Fine: {totalFine:N0} đ")
                        .Bold()
                        .FontSize(11)
                        .FontColor(
                            totalFine > 0
                                ? Colors.Red.Darken2
                                : Colors.Green.Darken2
                        );

                    col.Item()
                        .PaddingTop(5)
                        .Text($"Report Date: {DateTime.Now:dd/MM/yyyy}")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
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

}