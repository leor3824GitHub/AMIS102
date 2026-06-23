using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintInventoryCountForm;

/// <summary>
/// COA Inventory Count Form (Annex A), rendered from a physical-count session's
/// reconciliation rows. Lists every line with the book quantity (per Property Card)
/// against the physical count, leaving the "New Property No." and "Location/Whereabouts"
/// columns blank for completion during validation.
/// </summary>
internal sealed class InventoryCountFormPdfDocument(
    ReconciliationReportDto report,
    OrganizationProfileDto? org,
    string                  paperSize   = "a4",
    string                  orientation = "landscape",
    float                   marginMm    = 12f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"Inventory Count Form — {report.Code}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeBody);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().AlignRight().Text("Annex A").Italic().FontSize(8);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }
            col.Item().AlignCenter().Text("INVENTORY COUNT FORM").Bold().FontSize(11);

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("PPE Account Group: ").SemiBold().FontSize(8);
                    t.Span("____________________________").FontSize(8);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Sheet No. ").FontSize(8);
                    t.Span("____").FontSize(8);
                    t.Span(" of ").FontSize(8);
                    t.Span("____").FontSize(8);
                });
            });

            col.Item().Row(row =>
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

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(7);

        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);   // Article / Item
                c.RelativeColumn(3);   // Description
                c.RelativeColumn(2);   // Old Property No.
                c.RelativeColumn(2);   // New Property No. (validation)
                c.RelativeColumn(2);   // Unit of Measure
                c.ConstantColumn(55);  // Unit Value
                c.ConstantColumn(45);  // Qty per Property Card
                c.ConstantColumn(45);  // Qty per Physical Count
                c.RelativeColumn(2);   // Location / Whereabouts
                c.RelativeColumn(2);   // Condition
                c.RelativeColumn(3);   // Remarks
            });

            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Article/Item").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Old Property No. assigned").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("New Property No. assigned (To be filled up during validation)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit of Measure").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Value (₱)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Quantity per Property Card").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Quantity per Physical Count").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Location/Whereabouts").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Condition").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Remarks").Style(style);
            });

            foreach (var r in report.Rows)
            {
                table.Cell().Border(0.5f).Padding(2).Text(r.Article).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(string.Empty);
                table.Cell().Border(0.5f).Padding(2).Text(r.PropertyNo ?? "—").FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(string.Empty);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.Unit).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(r.UnitCost.ToString("N2")).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.BookQty.ToString()).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(r.CountedQty.ToString()).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(string.Empty);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(FormatCondition(r.Condition)).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(r.Remarks ?? string.Empty).FontSize(7);
            }

            if (report.Rows.Count == 0)
                table.Cell().ColumnSpan(11).Border(0.5f).Padding(6).AlignCenter()
                    .Text("No items counted for this session.").Italic().FontSize(8);
        });
    }

    private static string FormatCondition(PhysicalCountCondition? c) => c switch
    {
        PhysicalCountCondition.InGoodCondition => "Serviceable",
        PhysicalCountCondition.NeedingRepair   => "Needs Repair",
        PhysicalCountCondition.Unserviceable   => "Unserviceable",
        PhysicalCountCondition.Missing         => "Missing",
        PhysicalCountCondition.FoundAtStation  => "Found at Station",
        _                                      => string.Empty
    };

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Prepared by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text(org?.PropertyCustodianName ?? "________________________").Bold().FontSize(8);
                    cell.Item().Text("Printed Name and Signature").FontSize(7);
                    cell.Item().Text(string.IsNullOrWhiteSpace(org?.PropertyCustodianDesignation)
                        ? "Property Custodian" : org!.PropertyCustodianDesignation!).FontSize(7);
                });
                row.ConstantItem(40);
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Reviewed by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text(org?.AssistantRegionalManagerName ?? "________________________").Bold().FontSize(8);
                    cell.Item().Text("Printed Name and Signature").FontSize(7);
                    cell.Item().Text(string.IsNullOrWhiteSpace(org?.AssistantRegionalManagerDesignation)
                        ? "Assistant Approver" : org!.AssistantRegionalManagerDesignation!).FontSize(7);
                });
            });

            col.Item().PaddingTop(8).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }
}
