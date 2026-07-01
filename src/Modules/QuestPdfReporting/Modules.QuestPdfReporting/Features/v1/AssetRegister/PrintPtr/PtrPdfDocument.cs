using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPtr;

/// <summary>
/// Property Transfer Report (PTR) — a PPEIR rendered as a COA transfer form. All fields are sourced
/// from the issuance report document: From = issued-by, To = issued-to, transfer type = nature.
/// </summary>
internal sealed class PtrPdfDocument(
    IssuanceReportDocumentDto  report,
    OrganizationProfileDto?    org,
    string                     paperSize   = "a4",
    string                     orientation = "portrait",
    float                      marginMm    = 12f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "PTR",
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
            col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(9);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text("PROPERTY TRANSFER REPORT").Bold().FontSize(12);
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            col.Spacing(6);

            // PTR No / Date / Fund Cluster
            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t => { t.Span("PTR No.: ").SemiBold(); t.Span(report.ReportNo); });
                row.RelativeItem().Text(t => { t.Span("Fund Cluster: ").SemiBold(); t.Span(report.FundCluster); });
                row.RelativeItem().AlignRight().Text(t => { t.Span("Date: ").SemiBold(); t.Span(report.Date.ToString("MMMM d, yyyy")); });
            });

            // Transfer type
            col.Item().Text(t =>
            {
                t.Span("Transfer Type: ").SemiBold();
                t.Span(TransferTypeLabel(report.Nature));
            });

            // From / To officer blocks
            col.Item().Border(0.75f).Padding(4).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("From (Releasing Accountable Officer)").SemiBold().FontSize(8);
                    c.Item().Text(report.IssuedByName).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(report.IssuedByDesignation))
                        c.Item().Text(report.IssuedByDesignation).FontSize(8);
                });
                row.ConstantItem(12);
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("To (Receiving Accountable Officer)").SemiBold().FontSize(8);
                    c.Item().Text(report.IssuedToName).FontSize(9);
                    if (!string.IsNullOrWhiteSpace(report.IssuedToDesignation))
                        c.Item().Text(report.IssuedToDesignation).FontSize(8);
                    if (!string.IsNullOrWhiteSpace(report.IssuedToOfficeAddress))
                        c.Item().Text(report.IssuedToOfficeAddress).FontSize(8);
                });
            });

            // Item table
            col.Item().Element(ComposeItems);

            if (!string.IsNullOrWhiteSpace(report.Remarks))
                col.Item().PaddingTop(2).Text(t => { t.Span("Remarks: ").SemiBold(); t.Span(report.Remarks); });
        });
    }

    private void ComposeItems(IContainer container)
    {
        var style = TextStyle.Default.Bold().FontSize(8);

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(30);   // Item No.
                c.RelativeColumn(3);     // Property No.
                c.RelativeColumn(5);     // Description
                c.RelativeColumn(1);     // Unit
                c.ConstantColumn(70);    // Unit Cost
                c.ConstantColumn(75);    // Amount
            });

            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Item No.").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Property No.").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Cost (₱)").Style(style);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Amount (₱)").Style(style);
            });

            foreach (var l in report.Lines)
            {
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(l.ItemNo.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(l.PropertyNo).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(l.Description).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(l.Unit).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(l.UnitCost.ToString("N2")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(l.Amount.ToString("N2")).FontSize(8);
            }

            if (report.Lines.Count == 0)
                table.Cell().ColumnSpan(6).Border(0.5f).Padding(6).AlignCenter()
                    .Text("No items on this transfer.").Italic().FontSize(8);

            table.Cell().ColumnSpan(5).Border(0.5f).Padding(2).AlignRight().Text("Total").Style(style);
            table.Cell().Border(0.5f).Padding(2).AlignRight().Text($"₱{report.TotalAmount:N2}").Bold().FontSize(8);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(16).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Released by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text(report.IssuedByName).Bold().FontSize(8);
                    cell.Item().Text(report.IssuedByDesignation ?? "Releasing Officer").FontSize(7);
                });
                row.RelativeItem().AlignCenter().Column(cell =>
                {
                    cell.Item().AlignCenter().Text("Approved by:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignCenter().Text(report.ApprovedByName).Bold().FontSize(8);
                    cell.Item().AlignCenter().Text(report.ApprovedByDesignation ?? "Approving Official").FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().AlignRight().Text("Received by:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignRight().Text(report.IssuedToName).Bold().FontSize(8);
                    cell.Item().AlignRight().Text(report.IssuedToDesignation ?? "Receiving Officer").FontSize(7);
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

    private static string TransferTypeLabel(IssuanceNature nature) => nature switch
    {
        IssuanceNature.TransferCO  => "Transfer to Central Office",
        IssuanceNature.TransferRO  => "Transfer to Regional Office",
        IssuanceNature.TransferPO  => "Transfer to Provincial Office",
        IssuanceNature.Donation    => "Donation",
        IssuanceNature.Dumping     => "Dumping",
        IssuanceNature.Destruction => "Destruction",
        IssuanceNature.Sale        => "Sale",
        _                          => "Others",
    };
}
