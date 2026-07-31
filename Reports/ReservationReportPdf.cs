using LibraryManagement.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagement.Reports;

public class ReservationReportPdf : IDocument
{
    private readonly List<Reservation> _reservations;

    public ReservationReportPdf(List<Reservation> reservations)
    {
        _reservations = reservations;
    }

    public DocumentMetadata GetMetadata()
        => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());

            page.Margin(25);

            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(column =>
            {
                ComposeHeader(column);
            });

            page.Content().Layers(layers =>
            {
                layers.Layer().AlignCenter().AlignMiddle().Text("LIBRARY")
                    .FontSize(90)
                    .Bold()
                    .FontColor(Colors.Grey.Lighten3);

                layers.PrimaryLayer().Column(column =>
                {
                    column.Spacing(20);

                    ComposeSummary(column);

                    ComposeTable(column);

                    ComposeFooterSummary(column);

                    ComposeSignature(column);
                });
            });

            page.Footer()
                .PaddingTop(10)
                .BorderTop(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Row(row =>
                {
                    row.RelativeItem()
                        .Text("Library Management System")
                        .FontSize(9)
                        .SemiBold();

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

    //==================================================
    // HEADER
    //==================================================

    private void ComposeHeader(ColumnDescriptor column)
    {
        column.Item().Row(row =>
        {
            //row.ConstantItem(70)
            //    .Height(70)
            //    .Image("wwwroot/images/logo.png");

            row.RelativeItem()
                .Column(c =>
                {
                    c.Item()
                        .AlignCenter()
                        .Text("LIBRARY MANAGEMENT SYSTEM")
                        .FontSize(22)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    c.Item()
                        .AlignCenter()
                        .Text("RESERVATION REPORT")
                        .FontSize(16)
                        .Bold();

                    c.Item()
                        .AlignCenter()
                        .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
        });

        column.Item()
            .PaddingTop(8)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);
    }

    //==================================================
    // SUMMARY
    //==================================================

    private void ComposeSummary(ColumnDescriptor column)
    {
        int total = _reservations.Count;

        int waiting =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Waiting);

        int approved =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Approved);

        int completed =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Completed);

        int cancelled =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Cancelled);

        column.Item().Row(row =>
        {
            row.Spacing(10);

            SummaryCard(row.RelativeItem(),
                "TOTAL",
                total.ToString(),
                Colors.Blue.Darken2);

            SummaryCard(row.RelativeItem(),
                "WAITING",
                waiting.ToString(),
                Colors.Orange.Darken2);

            SummaryCard(row.RelativeItem(),
                "APPROVED",
                approved.ToString(),
                Colors.Blue.Medium);

            SummaryCard(row.RelativeItem(),
                "COMPLETED",
                completed.ToString(),
                Colors.Green.Darken2);

            SummaryCard(row.RelativeItem(),
                "CANCELLED",
                cancelled.ToString(),
                Colors.Red.Darken2);
        });
    }
    //==================================================
    // TABLE
    //==================================================

    private void ComposeTable(ColumnDescriptor column)
    {
        column.Item()
            .Text("RESERVATION DETAILS")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Blue.Darken2);

        column.Item().PaddingTop(8);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(35);     // No
                columns.RelativeColumn(1.6f);   // Customer
                columns.RelativeColumn(2f);     // Email
                columns.RelativeColumn(2f);     // Book
                columns.RelativeColumn(.8f);    // Qty
                columns.RelativeColumn(1.2f);   // Date
                columns.RelativeColumn(1.2f);   // Status
            });

            //================ HEADER ================

            HeaderCell(table.Cell(), "No");
            HeaderCell(table.Cell(), "Customer");
            HeaderCell(table.Cell(), "Email");
            HeaderCell(table.Cell(), "Book");
            HeaderCell(table.Cell(), "Qty");
            HeaderCell(table.Cell(), "Reservation");
            HeaderCell(table.Cell(), "Status");

            if (!_reservations.Any())
            {
                table.Cell()
                    .ColumnSpan(7)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(20)
                    .AlignCenter()
                    .Text("No reservation found.")
                    .Italic();

                return;
            }

            int no = 1;

            foreach (var item in _reservations)
            {
                string rowColor =
                    no % 2 == 0
                        ? Colors.Grey.Lighten5
                        : Colors.White;

                string statusText = item.Status.ToString();

                string statusColor =
                    item.Status switch
                    {
                        ReservationStatus.Waiting => Colors.Orange.Darken2,
                        ReservationStatus.Approved => Colors.Blue.Darken2,
                        ReservationStatus.Completed => Colors.Green.Darken2,
                        ReservationStatus.Cancelled => Colors.Red.Darken2,
                        _ => Colors.Grey.Darken2
                    };

                BodyCell(
                    table.Cell(),
                    no.ToString(),
                    rowColor,
                    true);

                BodyCell(
                    table.Cell(),
                    item.CustomerName,
                    rowColor);

                BodyCell(
                    table.Cell(),
                    item.CustomerEmail,
                    rowColor);

                BodyCell(
                    table.Cell(),
                    item.Book?.Title ?? "",
                    rowColor);

                BodyCell(
                    table.Cell(),
                    item.Quantity.ToString(),
                    rowColor,
                    true);

                BodyCell(
                    table.Cell(),
                    item.ReservationDate.ToString("dd/MM/yyyy"),
                    rowColor,
                    true);

                table.Cell()
                    .Background(rowColor)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(6)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(statusText)
                    .Bold()
                    .FontColor(statusColor);

                no++;
            }
        });
    }
    private void HeaderCell(IContainer container, string text)
    {
        container
            .Background(Colors.Blue.Darken2)
            .Border(1)
            .BorderColor(Colors.White)
            .Padding(7)
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
        var cell = container
            .Background(background)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(6);

        if (center)
            cell = cell.AlignCenter();
        else
            cell = cell.AlignLeft();

        cell.AlignMiddle();

        cell.Text(text);
    }
    private void SummaryCard(
    IContainer container,
    string title,
    string value,
    string color)
    {
        container
            .Border(1)
            .BorderColor(color)
            .Padding(10)
            .Column(col =>
            {
                col.Item()
                    .Text(title)
                    .Bold()
                    .FontSize(10)
                    .FontColor(color);

                col.Item()
                    .PaddingTop(5)
                    .Text(value)
                    .Bold()
                    .FontSize(22);
            });
    }
    private void ComposeFooterSummary(ColumnDescriptor column)
    {
        int total = _reservations.Count;

        int waiting =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Waiting);

        int approved =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Approved);

        int completed =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Completed);

        int cancelled =
            _reservations.Count(x =>
                x.Status == ReservationStatus.Cancelled);

        column.Item()
            .PaddingTop(10)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(10);

        column.Item()
            .Text("REPORT SUMMARY")
            .Bold()
            .FontSize(13)
            .FontColor(Colors.Blue.Darken2);

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
                    col.Item().Text($"Total Reservation : {total}");

                    col.Item()
                        .PaddingTop(4)
                        .Text($"Waiting : {waiting}");

                    col.Item()
                        .PaddingTop(4)
                        .Text($"Approved : {approved}");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Completed : {completed}");

                    col.Item()
                        .PaddingTop(4)
                        .Text($"Cancelled : {cancelled}");

                    col.Item()
                        .PaddingTop(4)
                        .Text($"Generated : {DateTime.Now:dd/MM/yyyy}");
                });
            });
    }
    private void ComposeSignature(ColumnDescriptor column)
    {
        column.Item().PaddingTop(20);

        column.Item()
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten2);

        column.Item().PaddingTop(15);

        column.Item()
            .AlignRight()
            .Text($"Date: {DateTime.Now:dd/MM/yyyy}")
            .FontSize(10)
            .Italic();

        column.Item().PaddingTop(10);

        column.Item().Row(row =>
        {
            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("PREPARED BY")
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    col.Item()
                        .Text("(Administrator)")
                        .FontSize(9);

                    col.Item().Height(55);

                    col.Item()
                        .AlignCenter()
                        .Width(140)
                        .LineHorizontal(1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Full name & signature)")
                        .FontSize(8)
                        .Italic();
                });

            row.ConstantItem(80);

            row.RelativeItem()
                .AlignCenter()
                .Column(col =>
                {
                    col.Item()
                        .Text("APPROVED BY")
                        .Bold()
                        .FontColor(Colors.Green.Darken2);

                    col.Item()
                        .Text("(Library Manager)")
                        .FontSize(9);

                    col.Item().Height(55);

                    col.Item()
                        .AlignCenter()
                        .Width(140)
                        .LineHorizontal(1);

                    col.Item()
                        .PaddingTop(5)
                        .Text("(Full name & signature)")
                        .FontSize(8)
                        .Italic();
                });
        });
    }
}