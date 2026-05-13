using Giwu.Contracts.Reports;
using Giwu.Domain.Reports;

namespace Giwu.Application.Reports.Persisted;

internal static class ReportDtoMapping
{
    public static ReportDefinitionDto ToDto(ReportDefinition d) =>
        new(d.Id.ToString(), d.Code, d.Name, d.ShortDescription, d.LongDescription,
            d.Category, IsCustom: true, d.RequiresDateRange, d.RequiresDepartmentFilter,
            ParseFormats(d.SupportedFormatsCsv),
            ParseColumns(d.ColumnsCsv),
            RegulatoryReference: null);

    public static ReportScheduleDto ToDto(ReportSchedule s) =>
        new(s.Id, s.DefinitionId, s.DefinitionName, s.Category, s.Frequency, s.Cadence,
            ParseList(s.RecipientsCsv), s.Format, s.NextRunAt, s.LastRunAt, s.IsActive);

    public static ReportRunDto ToDto(ReportRun r) =>
        new(r.Id, r.DefinitionId, r.DefinitionName, r.Category, r.Format,
            r.QueuedAt, r.CompletedAt, r.Status, r.RanByDisplay,
            r.PeriodStart, r.PeriodEnd, ParseList(r.DepartmentsCsv),
            r.RowCount, r.FileSizeBytes, r.FileName, r.ErrorMessage, r.ScheduleId);

    public static ComplianceDeadlineDto ToDto(ComplianceDeadline c) =>
        new(c.Id, c.Agency, c.FormCode, c.Name, c.Description, c.DueDate,
            c.PeriodCovered, c.IsFiled, c.FiledOn, c.RelatedReportCode);

    public static string JoinFormats(IReadOnlyList<ReportFormat>? formats) =>
        string.Join(",", (formats ?? Array.Empty<ReportFormat>()).Select(f => f.ToString()));

    public static IReadOnlyList<ReportFormat> ParseFormats(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<ReportFormat>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(s => Enum.TryParse<ReportFormat>(s.Trim(), true, out var v) ? v : ReportFormat.Csv)
                 .Distinct()
                 .ToList();

    public static IReadOnlyList<string> ParseColumns(string raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

    public static IReadOnlyList<string> ParseList(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
}
