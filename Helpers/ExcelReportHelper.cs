using ClosedXML.Excel;

namespace LibraryManagement.Helpers;

public static class ExcelReportHelper
{
    public static void ApplyTitle(
        IXLWorksheet ws,
        string title,
        int lastColumn)
    {
        var titleRange = ws.Range(1, 1, 1, lastColumn);

        titleRange.Merge();

        titleRange.Value = title;

        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 20;
        titleRange.Style.Font.FontColor = XLColor.White;

        titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D6EFD");

        titleRange.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        titleRange.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        ws.Row(1).Height = 30;
    }

    public static void ApplyInfo(
        IXLWorksheet ws,
        int totalRecord)
    {
        ws.Cell("A3").Value =
            $"Generated : {DateTime.Now:dd/MM/yyyy HH:mm}";

        ws.Cell("A4").Value =
            $"Total Records : {totalRecord}";

        ws.Range("A3:B4")
            .Style.Font.Bold = true;
    }

    public static void ApplyHeader(
        IXLWorksheet ws,
        int row,
        int lastColumn)
    {
        var range = ws.Range(row, 1, row, lastColumn);

        range.Style.Font.Bold = true;

        range.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#0D6EFD");

        range.Style.Font.FontColor =
            XLColor.White;

        range.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        range.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;
    }

    public static void ApplyBorder(
        IXLWorksheet ws,
        int firstRow,
        int lastRow,
        int lastColumn)
    {
        var range =
            ws.Range(firstRow, 1, lastRow, lastColumn);

        range.Style.Border.OutsideBorder =
            XLBorderStyleValues.Thin;

        range.Style.Border.InsideBorder =
            XLBorderStyleValues.Thin;
    }

    public static void ApplyAlternateColor(
        IXLWorksheet ws,
        int firstRow,
        int lastRow,
        int lastColumn)
    {
        for (int r = firstRow; r <= lastRow; r++)
        {
            if (r % 2 == 0)
            {
                ws.Range(r, 1, r, lastColumn)
                    .Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#F7F7F7");
            }
        }
    }

    public static void Finish(IXLWorksheet ws)
    {
        ws.Columns().AdjustToContents();

        ws.SheetView.FreezeRows(6);

        ws.RangeUsed().SetAutoFilter();
    }
}