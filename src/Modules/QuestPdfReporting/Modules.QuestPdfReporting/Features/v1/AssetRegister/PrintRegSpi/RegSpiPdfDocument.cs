using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Reports;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintRegSpi;

/// <summary>
/// Registry of Semi-Expendable Property Issued (RegSPI) — COA Annex A.4. One sheet per
/// Fund Cluster × SE classification; each row is one transaction (issue / return / re-issue /
/// disposal) filling exactly one of the form's column groups, with a running Balance column.
/// </summary>
internal sealed class RegSpiPdfDocument(
    RegSpiReportDto          report,
    OrganizationProfileDto?  org,
    string                   paperSize   = "legal",
    string                   orientation = "landscape",
    float                    marginMm    = 12f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "RegSPI",
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
            col.Item().AlignRight().Text("Annex A.4").Italic().FontSize(8);
            col.Item().AlignCenter().Text("REGISTRY OF SEMI-EXPENDABLE PROPERTY ISSUED").Bold().FontSize(11);
            col.Item().AlignCenter().Text($"As of {report.AsOfDate:MMMM d, yyyy}").FontSize(8);
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Column(col =>
        {
            if (report.TotalTransactions == 0)
            {
                col.Item().Border(0.5f).Padding(6).AlignCenter()
                    .Text("No semi-expendable property transactions as of this date.").Italic().FontSize(8);
                return;
            }

            // COA Annex A.4 scopes one sheet per Fund Cluster × SE classification — each sheet gets its
            // own header block (Entity Name / Fund Cluster / Semi-expendable Property / Sheet No.) and
            // its own ledger table, page-broken from the next.
            var sheets = report.Groups
                .SelectMany(fc => fc.Classifications.Select(cls => (fc.FundCluster, Sheet: cls)))
                .ToList();

            for (var si = 0; si < sheets.Count; si++)
            {
                var (fundCluster, sheet) = sheets[si];
                var fcLabel = string.IsNullOrWhiteSpace(fundCluster) ? "(Unspecified)" : fundCluster;

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(t =>
                        {
                            t.Span("Entity Name: ").FontSize(8);
                            t.Span(org?.Name ?? string.Empty).Bold().FontSize(8);
                        });
                        left.Item().Text(t =>
                        {
                            t.Span("Semi-expendable Property: ").FontSize(8);
                            t.Span(sheet.ClassificationName).Bold().FontSize(8);
                        });
                    });
                    row.ConstantItem(220).Column(right =>
                    {
                        right.Item().Text(t =>
                        {
                            t.Span("Fund Cluster: ").FontSize(8);
                            t.Span(fcLabel).Bold().FontSize(8);
                        });
                        right.Item().Text(t =>
                        {
                            t.Span("Sheet No.: ").FontSize(8);
                            t.Span(sheet.SheetNo.ToString()).Bold().FontSize(8);
                        });
                    });
                });

                col.Item().PaddingTop(4).Element(c => ComposeSheetTable(c, sheet));

                col.Item().PaddingTop(3).AlignRight().Text(t =>
                {
                    t.Span($"Issued: {sheet.IssuedQty}    Returned: {sheet.ReturnedQty}    " +
                           $"Re-issued: {sheet.ReissuedQty}    Disposed: {sheet.DisposedQty}    ").SemiBold().FontSize(8);
                    t.Span($"Balance: {sheet.BalanceQty} item(s)    ₱{sheet.BalanceAmount:N2}").Bold().FontSize(8);
                });

                if (si < sheets.Count - 1)
                    col.Item().PageBreak();
            }

            col.Item().PaddingTop(8).LineHorizontal(1);
            col.Item().PaddingTop(4).AlignRight().Text(t =>
            {
                t.Span($"GRAND TOTAL — balance: {report.BalanceQty} item(s)    ").SemiBold().FontSize(9);
                t.Span("amount: ").SemiBold().FontSize(9);
                t.Span($"₱{report.BalanceAmount:N2}").Bold().FontSize(9);
            });
        });
    }

    private static void ComposeSheetTable(IContainer container, RegSpiClassificationGroupDto sheet)
    {
        var headStyle = TextStyle.Default.Bold().FontSize(7.5f);

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(48);    //  1 Date
                c.RelativeColumn(2.3f);  //  2 ICS/RRSP No.
                c.RelativeColumn(2.3f);  //  3 Semi-expendable Property No.
                c.RelativeColumn(3.2f);  //  4 Item Description
                c.ConstantColumn(36);    //  5 Estimated Useful Life
                c.ConstantColumn(24);    //  6 Issued Qty
                c.RelativeColumn(2.6f);  //  7 Issued Office/Officer
                c.ConstantColumn(24);    //  8 Returned Qty
                c.RelativeColumn(2.6f);  //  9 Returned Office/Officer
                c.ConstantColumn(24);    // 10 Re-issued Qty
                c.RelativeColumn(2.6f);  // 11 Re-issued Office/Officer
                c.ConstantColumn(28);    // 12 Disposed Qty
                c.ConstantColumn(28);    // 13 Balance Qty
                c.ConstantColumn(56);    // 14 Amount
                c.RelativeColumn(2.0f);  // 15 Remarks
            });

            // Two-tier Annex A.4 header: grouped columns (Reference / Issued / Returned / Re-issued /
            // Disposed / Balance) with Qty + Office/Officer sub-columns. Repeats on every page.
            table.Header(h =>
            {
                static IContainer HeadCell(IContainer c) =>
                    c.Border(1).Padding(2).AlignCenter().AlignMiddle();

                h.Cell().RowSpan(2).Element(HeadCell).Text("Date").Style(headStyle);
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("Reference").Style(headStyle);
                h.Cell().RowSpan(2).Element(HeadCell).Text("Item Description").Style(headStyle);
                h.Cell().RowSpan(2).Element(HeadCell).Text("Estimated Useful Life").Style(headStyle);
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("Issued").Style(headStyle);
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("Returned").Style(headStyle);
                h.Cell().ColumnSpan(2).Element(HeadCell).Text("Re-issued").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Disposed").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Balance").Style(headStyle);
                h.Cell().RowSpan(2).Element(HeadCell).Text("Amount (₱)").Style(headStyle);
                h.Cell().RowSpan(2).Element(HeadCell).Text("Remarks").Style(headStyle);

                h.Cell().Element(HeadCell).Text("ICS/RRSP No.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Semi-expendable Property No.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Qty.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Office/Officer").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Qty.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Office/Officer").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Qty.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Office/Officer").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Qty.").Style(headStyle);
                h.Cell().Element(HeadCell).Text("Qty.").Style(headStyle);
            });

            foreach (var r in sheet.Rows)
            {
                static IContainer BodyCell(IContainer c) => c.Border(0.5f).Padding(2);

                var issued   = r.TransactionType == RegSpiTransactionType.Issued;
                var returned = r.TransactionType == RegSpiTransactionType.Returned;
                var reissued = r.TransactionType == RegSpiTransactionType.Reissued;
                var disposed = r.TransactionType == RegSpiTransactionType.Disposed;

                table.Cell().Element(BodyCell).AlignCenter().Text(r.Date.ToString("yyyy-MM-dd")).FontSize(7);
                table.Cell().Element(BodyCell).Text(r.ReferenceNo).FontSize(7);
                table.Cell().Element(BodyCell).Text(r.PropertyNo).FontSize(7);
                table.Cell().Element(BodyCell).Text(r.Description).FontSize(7);
                table.Cell().Element(BodyCell).AlignCenter().Text($"{r.EstimatedUsefulLifeYears} yr(s)").FontSize(7);

                table.Cell().Element(BodyCell).AlignCenter().Text(issued ? r.Qty.ToString() : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).Text(issued ? r.OfficeOfficer ?? string.Empty : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).AlignCenter().Text(returned ? r.Qty.ToString() : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).Text(returned ? r.OfficeOfficer ?? string.Empty : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).AlignCenter().Text(reissued ? r.Qty.ToString() : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).Text(reissued ? r.OfficeOfficer ?? string.Empty : string.Empty).FontSize(7);
                table.Cell().Element(BodyCell).AlignCenter().Text(disposed ? r.Qty.ToString() : string.Empty).FontSize(7);

                table.Cell().Element(BodyCell).AlignCenter().Text(r.Balance.ToString()).FontSize(7);
                table.Cell().Element(BodyCell).AlignRight().Text(r.Amount.ToString("N2")).FontSize(7);
                table.Cell().Element(BodyCell).Text(r.Remarks ?? string.Empty).FontSize(7);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(14).Row(row =>
            {
                row.RelativeItem().Column(cell =>
                {
                    cell.Item().Text("Prepared by:").FontSize(8);
                    cell.Item().PaddingTop(20).Text("________________________").FontSize(8);
                    cell.Item().Text("Supply / Property Officer").FontSize(7);
                });
                row.RelativeItem().AlignRight().Column(cell =>
                {
                    cell.Item().Text("Certified Correct:").FontSize(8);
                    cell.Item().PaddingTop(20).AlignRight().Text(org?.AccountantName ?? "________________________").Bold().FontSize(8);
                    cell.Item().AlignRight().Text(org?.AccountantDesignation ?? "Accountant").FontSize(7);
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
