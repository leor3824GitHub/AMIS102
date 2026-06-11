using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;

/// <summary>
/// COA physical-count supporting annex (B = Found at Station, C = Non-Existing/Missing),
/// rendered from a session's reconciliation rows.
/// </summary>
internal sealed class CountAnnexPdfDocument(
    ReconciliationReportDto    report,
    OrganizationProfileDto?    org,
    List<ReportSignatoryDto>   signatories,
    CountAnnexKind             annex,
    string                     paperSize   = "a4",
    string                     orientation = "portrait",
    float                      marginMm    = 15f) : IDocument
{
    private bool IsFoundAtStation => annex == CountAnnexKind.FoundAtStation;

    private string Title => IsFoundAtStation
        ? "LIST OF PROPERTY, PLANT AND EQUIPMENT FOUND AT STATION"
        : "LIST OF NON-EXISTING / MISSING PROPERTY, PLANT AND EQUIPMENT";

    private string AnnexLabel => IsFoundAtStation ? "Annex B" : "Annex C";

    private IReadOnlyList<ReconciliationRowDto> Rows => IsFoundAtStation
        ? report.Rows.Where(r => r.RowStatus == ReconciliationRowStatus.Overage).ToList()
        : report.Rows.Where(r => r.RowStatus is ReconciliationRowStatus.Shortage
                                              or ReconciliationRowStatus.Uncounted).ToList();

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"{AnnexLabel} — {Title}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeBody);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignRight().Text(AnnexLabel).Italic().FontSize(8);
            col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(9);
            col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(10);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text(Title).Bold().FontSize(11);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Fund Cluster: ").SemiBold().FontSize(8);
                    t.Span(report.FundCluster).FontSize(8);
                });
                row.RelativeItem().AlignCenter().Text(t =>
                {
                    t.Span("Count Ref.: ").SemiBold().FontSize(8);
                    t.Span(report.Code).FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Office Order No.: ").SemiBold().FontSize(8);
                    t.Span(string.IsNullOrWhiteSpace(report.OfficeOrderNo) ? "—" : report.OfficeOrderNo).FontSize(8);
                });
            });
            if (report.FrozenOnUtc is { } frozen)
            {
                col.Item().Text(t =>
                {
                    t.Span("As at: ").SemiBold().FontSize(8);
                    t.Span(frozen.LocalDateTime.ToString("MMMM d, yyyy")).FontSize(8);
                });
            }

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var qtyHeader = IsFoundAtStation ? "Qty on Hand" : "Qty per Card";
        var style = TextStyle.Default.Bold().FontSize(8);

        container.PaddingTop(4).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);   // No.
                    c.RelativeColumn(3);     // Property No.
                    c.RelativeColumn(5);     // Article / Description
                    c.RelativeColumn(2);     // Unit
                    c.ConstantColumn(70);    // Unit Cost
                    c.ConstantColumn(55);    // Qty
                    c.RelativeColumn(3);     // Remarks
                });

                table.Header(h =>
                {
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Article / Description").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Value (₱)").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text(qtyHeader).Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Style(style);
                });

                var rows = Rows;
                var no = 1;
                foreach (var r in rows)
                {
                    var qty = IsFoundAtStation ? r.CountedQty : r.BookQty;
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(no.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.PropertyNo ?? "—").FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(r.Article).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Unit).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(r.UnitCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(qty.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(RowRemark(r)).FontSize(8);
                    no++;
                }

                if (rows.Count == 0)
                {
                    table.Cell().ColumnSpan(7).Border(0.5f).Padding(6).AlignCenter()
                        .Text(IsFoundAtStation
                            ? "No property found at station for this count."
                            : "No non-existing / missing property for this count.")
                        .Italic().FontSize(8);
                }
            });

            var totalValue = IsFoundAtStation ? report.OverageValue : report.ShortageValue;
            col.Item().PaddingTop(4).AlignRight().Text(t =>
            {
                t.Span($"Total {(IsFoundAtStation ? "found-at-station" : "missing")} value: ").SemiBold().FontSize(9);
                t.Span($"₱{totalValue:N2}").Bold().FontSize(9);
            });
        });
    }

    private string RowRemark(ReconciliationRowDto r)
    {
        if (!string.IsNullOrWhiteSpace(r.Remarks)) return r.Remarks!;
        if (IsFoundAtStation) return "Found at station";
        return r.RowStatus == ReconciliationRowStatus.Uncounted ? "On books, not counted" : "Recorded missing";
    }

    private void ComposeFooter(IContainer container)
    {
        var active = signatories.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToList();
        var approved = active.FirstOrDefault(s => s.Label.Contains("APPROVED", StringComparison.OrdinalIgnoreCase));
        var members = active.Where(s => s != approved).ToList();

        container.Column(col =>
        {
            col.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Certified Correct by:").FontSize(8);
                    foreach (var m in members.Take(3))
                    {
                        cell.Item().PaddingTop(14).Text(m.Name).Bold().FontSize(8);
                        cell.Item().Text(string.IsNullOrWhiteSpace(m.Title) ? "Inventory Committee" : m.Title).FontSize(7);
                    }
                    if (members.Count == 0)
                        cell.Item().PaddingTop(20).Text("________________________").FontSize(8);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Approved by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text(approved?.Name ?? "________________________").Bold().FontSize(8);
                    cell.Item().Text(approved?.Title ?? "Head of Agency / Authorized Official").FontSize(7);
                });
            });

            col.Item().PaddingTop(6).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }
}
