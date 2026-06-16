using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.ReturnedProperty;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintReturnedProperty;

/// <summary>
/// Receipt of Returned Property — NFA Exhibit 6 (SOP GS-PD16).
/// Serves both RRP (PPE / PAR) and RRSP (SE / ICS); the receipt type drives the
/// title, the document-number label and the accountability column header (PAR No. / ICS No.).
/// </summary>
internal sealed class ReturnedPropertyPdfDocument(
    ReturnedPropertyReceiptDto receipt,
    OrganizationProfileDto?    org,
    string                     paperSize   = "longbond",
    string                     orientation = "portrait",
    float                      marginMm    = 14f,
    int                        minRows     = 12) : IDocument
{
    private bool IsRrp => receipt.ReceiptType == ReturnedPropertyReceiptType.RRP;
    private string Acronym => IsRrp ? "RRP" : "RRSP";
    private string Title => IsRrp
        ? "RECEIPT OF RETURNED PROPERTY"
        : "RECEIPT OF RETURNED SEMI-EXPENDABLE PROPERTY";
    private string AccountabilityColHeader => IsRrp ? "PAR No." : "ICS No.";

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = $"{Acronym} — {receipt.ReceiptNo ?? "(pending)"}",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation, marginMm);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
            page.Content().Element(ComposeBody);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.Column(col =>
        {
            // SOP / Exhibit identifiers
            col.Item().Text("SOP Number : GS-PD16").Bold().FontSize(9);
            col.Item().AlignCenter().Text("EXHIBIT 6").Italic().FontSize(9);

            // Agency header
            col.Item().PaddingTop(2).AlignCenter().Text("Republic of the Philippines").FontSize(9);
            col.Item().AlignCenter().Text("NATIONAL FOOD AUTHORITY").Bold().FontSize(10);
            if (org is not null)
            {
                if (!string.IsNullOrWhiteSpace(org.Name))
                    col.Item().AlignCenter().Text(org.Name).Bold().FontSize(10);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }

            // Title + document number row
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem(3).AlignCenter().Text(Title).Bold().FontSize(12);
                row.RelativeItem(1).Border(1).Padding(3).Column(c =>
                {
                    c.Item().Text($"{Acronym} NUMBER").SemiBold().FontSize(8);
                    c.Item().Text(receipt.ReceiptNo ?? string.Empty).Bold().FontSize(9);
                });
            });

            // Received-from / Office / Date-received
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("RECEIVED FROM: ").SemiBold().FontSize(8);
                    t.Span(receipt.ReturnedBy.PrintedName).FontSize(8);
                });
                row.RelativeItem().Text(t =>
                {
                    t.Span("OFFICE: ").SemiBold().FontSize(8);
                    t.Span(receipt.ReturnedBy.Designation ?? string.Empty).FontSize(8);
                });
                row.RelativeItem().Text(t =>
                {
                    t.Span("DATE RECEIVED: ").SemiBold().FontSize(8);
                    t.Span(receipt.Date.ToString("MM/dd/yyyy")).FontSize(8);
                });
            });

            col.Item().PaddingTop(4).Element(ComposeItemsTable);

            col.Item().Element(ComposeSignatures);
            col.Item().Element(ComposeInspectionReport);
            col.Item().Element(ComposeCopyDistribution);
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        var headStyle = TextStyle.Default.Bold().FontSize(7);

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);    // PAR No. / ICS No.
                c.RelativeColumn(2);    // Property Code
                c.RelativeColumn(4);    // Description
                c.RelativeColumn(2);    // Date Acquired
                c.RelativeColumn(1);    // Qty
                c.RelativeColumn(2);    // Unit Cost
                c.RelativeColumn(2);    // Total Cost
                c.RelativeColumn(2);    // Net Book Value
            });

            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).AlignCenter().Text(AccountabilityColHeader).Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("PROPERTY CODE").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("DESCRIPTION").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("DATE ACQUIRED").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("QTY").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("UNIT COST").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("TOTAL COST").Style(headStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("NET BOOK VALUE").Style(headStyle);
            });

            foreach (var item in receipt.Items.OrderBy(i => i.ItemNo))
            {
                var snap = item.Snapshot;
                // Each returned item is a single tracked asset → quantity is 1.
                const int qty = 1;
                var total = snap.UnitCost * qty;

                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(receipt.AccountabilityDocumentNo).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(snap.PropertyNo).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(snap.Description).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(snap.AcquisitionDate.ToString("MM/dd/yyyy")).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(qty.ToString()).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(snap.UnitCost.ToString("N2")).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text(total.ToString("N2")).FontSize(7);
                // Net book value is not carried on the snapshot — left blank for manual entry.
                table.Cell().Border(0.5f).Padding(2).Text(string.Empty);
            }

            // Pad with empty rows so the form keeps its fixed-grid appearance.
            for (var r = receipt.Items.Count; r < minRows; r++)
                for (var ci = 0; ci < 8; ci++)
                    table.Cell().Border(0.5f).Padding(2).MinHeight(14).Text(string.Empty);

            // Totals row.
            var grandTotal = receipt.Items.Sum(i => i.Snapshot.UnitCost);
            table.Cell().ColumnSpan(6).Border(0.5f).Padding(2).AlignRight().Text("TOTAL").Bold().FontSize(7);
            table.Cell().Border(0.5f).Padding(2).AlignRight().Text(grandTotal.ToString("N2")).Bold().FontSize(7);
            table.Cell().Border(0.5f).Padding(2).Text(string.Empty);
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.PaddingTop(10).Row(row =>
        {
            row.RelativeItem().Column(cell =>
            {
                cell.Item().Text("NOTED BY:").SemiBold().FontSize(8);
                cell.Item().PaddingTop(16).Text(org?.ApprovingOfficialName ?? string.Empty).Bold().FontSize(9);
                cell.Item().LineHorizontal(0.5f);
                cell.Item().Text(org?.ApprovingOfficialDesignation ?? "Chief, PSMD / RM / PM").FontSize(7);
            });
            row.ConstantItem(30);
            row.RelativeItem().Column(cell =>
            {
                cell.Item().Text("RECEIVED BY:").SemiBold().FontSize(8);
                cell.Item().PaddingTop(16).Text(receipt.ReceivedBy?.PrintedName ?? string.Empty).Bold().FontSize(9);
                cell.Item().LineHorizontal(0.5f);
                cell.Item().Text(receipt.ReceivedBy?.Designation ?? "Chief, PMSDS / Supply Officer").FontSize(7);
            });
        });
    }

    private void ComposeInspectionReport(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Item().LineHorizontal(0.5f);
            col.Item().PaddingTop(4).AlignCenter().Text("INSPECTION REPORT").Bold().FontSize(10);
            col.Item().PaddingTop(4).Text("Findings and Recommendations:").SemiBold().FontSize(8);
            col.Item().MinHeight(36).Text(receipt.InspectionRemarks ?? string.Empty).FontSize(8);

            col.Item().PaddingTop(12).AlignRight().Column(cell =>
            {
                cell.Item().Width(180).Text(receipt.InspectedBy?.PrintedName ?? string.Empty).Bold().FontSize(9);
                cell.Item().Width(180).LineHorizontal(0.5f);
                cell.Item().Width(180).Text(receipt.InspectedBy?.Designation ?? "Property Inspector").FontSize(7);
            });
        });
    }

    private static void ComposeCopyDistribution(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            col.Item().LineHorizontal(0.5f);
            col.Item().PaddingTop(2).Text("Copy 1 — Employee").FontSize(7);
            col.Item().Text("     2 — PMMS").FontSize(7);
            col.Item().Text("     3 — PMSDS").FontSize(7);
            col.Item().Text("     4 — DAS Gen. Acctg. (for junked assets)").FontSize(7);
            col.Item().Text("     5 — DAS IBC (for junked assets)").FontSize(7);
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignRight().Text(x =>
        {
            x.Span("Page ").FontSize(7);
            x.CurrentPageNumber().FontSize(7);
            x.Span(" of ").FontSize(7);
            x.TotalPages().FontSize(7);
        });
    }
}
