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

    /// <summary>
    /// Branded PDF export. The header carries the tenant's company name + an
    /// accent-coloured rule; the body is a zebra-striped table; the footer has
    /// the timestamp on the left and page numbers on the right.
    /// </summary>
    public static byte[] BuildPdf(
        string title,
        string csv,
        string companyName,
        string? periodText = null,
        string? accentHex = null)
    {
        var rows   = ParseCsv(csv);
        var header = rows.Count > 0 ? rows[0] : Array.Empty<string>();
        var data   = rows.Count > 1 ? rows.GetRange(1, rows.Count - 1) : new List<string[]>();

        var accent      = NormalizeHex(accentHex) ?? "#1D9E75";
        var accentLight = LightenHex(accent, 0.92f); // tinted band for header row
        var company     = string.IsNullOrWhiteSpace(companyName) ? "Giwu HRMS" : companyName.Trim();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(36);                            // generous breathing room
                page.PageColor(PdfColors.White);
                page.DefaultTextStyle(t => t
                    .FontSize(9.5f)
                    .FontColor(PdfColors.Grey.Darken4));

                // ── Branded header ────────────────────────────────────────
                page.Header().PaddingBottom(14).Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text(company)
                                .FontSize(10).SemiBold().FontColor(accent).LetterSpacing(0.4f);
                            c.Item().PaddingTop(2).Text(title)
                                .FontSize(18).Bold().FontColor(PdfColors.Grey.Darken4);
                        });
                        r.ConstantItem(170).AlignRight().Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(periodText))
                            {
                                c.Item().Text("Period").FontSize(7.5f)
                                    .FontColor(PdfColors.Grey.Medium).LetterSpacing(0.4f);
                                c.Item().Text(periodText)
                                    .FontSize(10).FontColor(PdfColors.Grey.Darken3);
                                c.Item().PaddingTop(2);
                            }
                            c.Item().Text("Generated").FontSize(7.5f)
                                .FontColor(PdfColors.Grey.Medium).LetterSpacing(0.4f);
                            c.Item().Text($"{DateTime.Now:MMM d, yyyy · h:mm tt}")
                                .FontSize(9.5f).FontColor(PdfColors.Grey.Darken3);
                        });
                    });

                    // Accent rule — subtle brand cue separating header from data.
                    col.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(accent);
                });

                // ── Content table ─────────────────────────────────────────
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < Math.Max(1, header.Length); i++)
                            cols.RelativeColumn();
                    });

                    // Table header
                    foreach (var h in header)
                        table.Cell()
                            .Background(accentLight)
                            .BorderBottom(1).BorderColor(accent)
                            .PaddingVertical(7).PaddingHorizontal(8)
                            .Text(h).SemiBold().FontSize(9).FontColor(PdfColors.Grey.Darken4);

                    // Data rows with zebra striping
                    for (int r = 0; r < data.Count; r++)
                    {
                        var row = data[r];
                        var rowBg = r % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten4;
                        for (int c = 0; c < header.Length; c++)
                        {
                            table.Cell()
                                .Background(rowBg)
                                .BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2)
                                .PaddingVertical(5).PaddingHorizontal(8)
                                .Text(c < row.Length ? row[c] : "")
                                .FontSize(9);
                        }
                    }
                });

                // ── Footer: company on the left, page count on the right ──
                page.Footer().PaddingTop(8).Row(r =>
                {
                    r.RelativeItem().Text(company)
                        .FontSize(8).FontColor(PdfColors.Grey.Medium);
                    r.RelativeItem().AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(8).FontColor(PdfColors.Grey.Medium));
                        t.Span("Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Hex utilities ─────────────────────────────────────────────────────
    private static string? NormalizeHex(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();
        if (!s.StartsWith('#')) s = "#" + s;
        return s.Length == 7 ? s : null; // tolerate only #RRGGBB
    }

    /// <summary>Returns a pastel variant of <paramref name="hex"/> by blending
    /// it with white at <paramref name="amount"/> (0 = same, 1 = white).</summary>
    private static string LightenHex(string hex, float amount)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length != 7) return "#F1F3F5";
        try
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            r = (int)(r + (255 - r) * amount);
            g = (int)(g + (255 - g) * amount);
            b = (int)(b + (255 - b) * amount);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return "#F1F3F5";
        }
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
