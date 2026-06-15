using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPhysicalCountReport;

/// <summary>
/// COA Report on the Physical Count of PPE (RPCPPE) or Semi-Expendable Property (RPCSEMEX),
/// rendered from the AssetRegister physical-count report view.
/// </summary>
internal sealed class PhysicalCountReportPdfDocument(
    PhysicalCountReportDto    report,
    AssetType                 assetType,
    OrganizationProfileDto?   org,
    List<ReportSignatoryDto>  signatories,
    string                    paperSize   = "a4",
    string                    orientation = "landscape",
    float                     marginMm    = 12f) : IDocument
{
    private bool IsPpe => assetType == AssetType.PPE;

    private string Title => IsPpe
        ? "REPORT ON THE PHYSICAL COUNT OF PROPERTY, PLANT AND EQUIPMENT"
        : "REPORT ON THE PHYSICAL COUNT OF SEMI-EXPENDABLE PROPERTY";

    private string Acronym => IsPpe ? "RPCPPE" : "RPCSEMEX";

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"{Acronym} — {report.Code}",
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
            col.Item().AlignRight().Text(Acronym).Italic().FontSize(8);
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
                    t.Span("As at: ").SemiBold().FontSize(8);
                    t.Span(report.AsAt.ToString("MMMM d, yyyy")).FontSize(8);
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(8);

        container.PaddingTop(4).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(26);   // No.
                    c.RelativeColumn(4);     // Article / Description
                    c.RelativeColumn(3);     // Property No.
                    c.RelativeColumn(2);     // Unit
                    c.ConstantColumn(75);    // Unit Value
                    c.RelativeColumn(2);     // Condition
                    c.RelativeColumn(3);     // Remarks
                });

                table.Header(h =>
                {
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Article / Description").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Value (₱)").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Condition").Style(style);
                    h.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Style(style);
                });

                var no = 1;
                foreach (var e in report.Entries)
                {
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(no.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(e.Article).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(e.PropertyNo ?? "—").FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(e.Unit).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(e.UnitCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(FormatCondition(e.Condition)).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(e.Remarks ?? string.Empty).FontSize(8);
                    no++;
                }

                if (report.Entries.Count == 0)
                    table.Cell().ColumnSpan(7).Border(0.5f).Padding(6).AlignCenter()
                        .Text("No items counted for this asset type.").Italic().FontSize(8);
            });

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Counted: ").SemiBold().FontSize(8);
                    t.Span(report.TotalEntries.ToString()).FontSize(8);
                    t.Span("   Missing: ").SemiBold().FontSize(8);
                    t.Span(report.TotalMissing.ToString()).FontSize(8);
                    t.Span("   Unserviceable: ").SemiBold().FontSize(8);
                    t.Span(report.TotalUnserviceable.ToString()).FontSize(8);
                    t.Span("   Found at Station: ").SemiBold().FontSize(8);
                    t.Span(report.TotalFoundAtStation.ToString()).FontSize(8);
                });
                if (IsPpe)
                {
                    row.RelativeItem().AlignRight().Text(t =>
                    {
                        t.Span("Total Book Value: ").SemiBold().FontSize(9);
                        t.Span($"₱{report.TotalBookValue:N2}").Bold().FontSize(9);
                    });
                }
            });
        });
    }

    private static string FormatCondition(PhysicalCountCondition c) => c switch
    {
        PhysicalCountCondition.InGoodCondition => "Serviceable",
        PhysicalCountCondition.NeedingRepair => "Needs Repair",
        PhysicalCountCondition.Unserviceable => "Unserviceable",
        PhysicalCountCondition.Missing => "Missing",
        PhysicalCountCondition.FoundAtStation => "Found at Station",
        _ => c.ToString()
    };

    private void ComposeFooter(IContainer container)
    {
        var active = signatories.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToList();
        var approved = active.FirstOrDefault(s => s.Label.Contains("APPROVED", StringComparison.OrdinalIgnoreCase));
        var verified = active.FirstOrDefault(s => s.Label.Contains("VERIFIED", StringComparison.OrdinalIgnoreCase));
        var members = active.Where(s => s != approved && s != verified).ToList();

        container.Column(col =>
        {
            col.Item().PaddingTop(12).Row(row =>
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
                row.RelativeItem().AlignCenter().Column(cell =>
                {
                    cell.Item().Text("Approved by:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignCenter().Text(approved?.Name ?? "________________________").Bold().FontSize(8);
                    cell.Item().AlignCenter().Text(approved?.Title ?? "Head of Agency").FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Verified by:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignRight().Text(verified?.Name ?? "________________________").Bold().FontSize(8);
                    cell.Item().AlignRight().Text(verified?.Title ?? "COA Representative").FontSize(7);
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
