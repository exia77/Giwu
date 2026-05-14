using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
// QuestPDF.Helpers.Colors collides with Microsoft.Maui.Graphics.Colors
// (pulled in by a global using). Alias to disambiguate at the call sites.
using PdfColors = QuestPDF.Helpers.Colors;

namespace Giwu.HRMS.Hybrid.Components.Pages.Reports;

/// <summary>
/// Converts the CSV-shaped report data produced by Reports.razor into real
/// .xlsx (ClosedXML) and .pdf (QuestPDF) byte buffers so the Download button
/// honors the user's chosen Format instead of always emitting CSV.
/// </summary>
internal static class ReportExporter
{
    public static byte[] BuildExcel(string title, string csv)
    {
        var rows = ParseCsv(csv);
        using var wb = new XLWorkbook();
        // Worksheet names have a 31-char cap and disallow []:*?/\
        var sheetName = Sanitize(title, 31);
        var ws = wb.AddWorksheet(string.IsNullOrWhiteSpace(sheetName) ? "Report" : sheetName);

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
                ws.Cell(r + 1, c + 1).Value = rows[r][c];
        }

        if (rows.Count > 0)
        {
            var header = ws.Range(1, 1, 1, rows[0].Length);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E1F5EE");
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public static byte[] BuildPdf(string title, string csv)
    {
        var rows = ParseCsv(csv);
        var header = rows.Count > 0 ? rows[0] : Array.Empty<string>();
        var data   = rows.Count > 1 ? rows.GetRange(1, rows.Count - 1) : new List<string[]>();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(title).FontSize(14).SemiBold();
                    col.Item().Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(8).FontColor(PdfColors.Grey.Darken2);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < Math.Max(1, header.Length); i++)
                            cols.RelativeColumn();
                    });

                    foreach (var h in header)
                        table.Cell().Background(PdfColors.Grey.Lighten3).Padding(4).Text(h).SemiBold();

                    foreach (var row in data)
                    {
                        for (int c = 0; c < header.Length; c++)
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(3)
                                .Text(c < row.Length ? row[c] : "");
                    }
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Page ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public static string MimeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".pdf"  => "application/pdf",
        ".csv"  => "text/csv",
        _       => "application/octet-stream",
    };

    // Minimal RFC-4180-ish CSV parser — handles quoted fields, escaped quotes,
    // and embedded newlines. Good enough for the data we emit ourselves.
    private static List<string[]> ParseCsv(string csv)
    {
        var rows = new List<string[]>();
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        void EndField() { fields.Add(sb.ToString()); sb.Clear(); }
        void EndRow()   { EndField(); rows.Add(fields.ToArray()); fields.Clear(); }

        for (int i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == ',') EndField();
                else if (ch == '\r') { /* skip; \n handles row break */ }
                else if (ch == '\n') EndRow();
                else sb.Append(ch);
            }
        }
        // Trailing field/row (file may not end with newline)
        if (sb.Length > 0 || fields.Count > 0) EndRow();
        return rows;
    }

    private static string Sanitize(string input, int maxLen)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var clean = new string(input.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
        return clean.Length > maxLen ? clean[..maxLen] : clean;
    }
}
