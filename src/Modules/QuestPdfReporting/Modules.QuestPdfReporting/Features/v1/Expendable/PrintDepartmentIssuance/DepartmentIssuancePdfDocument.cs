using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;

internal sealed class DepartmentIssuancePdfDocument(
    List<DepartmentIssuanceSummaryDto> data,
    OrganizationProfileDto?            org,
    List<ReportSignatoryDto>           signatories,
    DateTimeOffset?                    from,
    DateTimeOffset?                    to,
    Dictionary<string, string>         departmentNames,
    string                             paperSize   = "a4",
    string                             orientation = "landscape") : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "Report of Supplies and Materials Issued",
        Author = org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            QuestPdfPaperSize.Apply(page, paperSize, orientation);
            page.Margin(1.5f, Unit.Centimetre);
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
            col.Item().AlignRight().Text("Appendix 64").Italic().FontSize(8);

            col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(9);
            if (org is not null)
            {
                col.Item().AlignCenter().Text(org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(org.Address))
                    col.Item().AlignCenter().Text(org.Address).FontSize(8);
            }

            col.Item().PaddingTop(6).AlignCenter()
                .Text("REPORT OF SUPPLIES AND MATERIALS ISSUED")
                .Bold().FontSize(11).LetterSpacing(1);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(); c.RelativeColumn();
                    c.RelativeColumn(); c.RelativeColumn();
                });
                table.Cell().Text("Entity Name:").Bold();
                table.Cell().Text(org?.Name ?? string.Empty);
                table.Cell().Text("Serial No.:").Bold();
                table.Cell().Text(BuildSerialNo());
                table.Cell().Text("Date:").Bold();
                table.Cell().Text(BuildDateRange());
            });

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);
                c.RelativeColumn(3);
                c.RelativeColumn(7);
                c.RelativeColumn(2);
                c.ConstantColumn(40);
                c.RelativeColumn(2.5f);
                c.RelativeColumn(2.5f);
            });

            table.Header(header =>
            {
                var style = TextStyle.Default.Bold().FontSize(8);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("#").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Stock No.").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Description").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Unit").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Qty Issued").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Unit Cost").Style(style);
                header.Cell().Border(1).Padding(2).AlignCenter().Text("Amount").Style(style);
            });

            var rowNum = 1;
            foreach (var dept in data)
            {
                var deptName = departmentNames.TryGetValue(dept.DepartmentId, out var n) ? n : dept.DepartmentId;

                table.Cell().ColumnSpan(7).BorderLeft(1).BorderRight(1).BorderBottom(0.5f)
                    .Background(Colors.Blue.Lighten4).Padding(2)
                    .Text(deptName).Bold().FontSize(8);

                foreach (var item in dept.Products)
                {
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(rowNum.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(item.ProductCode).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).Text(item.ProductName).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(item.Unit).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(item.TotalQuantityIssued.ToString()).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(item.UnitCost.ToString("N2")).FontSize(8);
                    table.Cell().Border(0.5f).Padding(2).AlignRight().Text(item.TotalValue.ToString("N2")).FontSize(8);
                    rowNum++;
                }

                table.Cell().ColumnSpan(4).BorderLeft(1).BorderRight(0.5f).BorderTop(1).BorderBottom(0.5f)
                    .Background(Colors.Grey.Lighten3).Padding(2)
                    .AlignRight().Text($"Subtotal — {deptName}").Bold().FontSize(8);
                table.Cell().BorderLeft(0.5f).BorderRight(0.5f).BorderTop(1).BorderBottom(0.5f)
                    .Background(Colors.Grey.Lighten3).Padding(2)
                    .AlignRight().Text(dept.TotalItemsIssued.ToString()).Bold().FontSize(8);
                table.Cell().BorderLeft(0.5f).BorderRight(0.5f).BorderTop(1).BorderBottom(0.5f)
                    .Background(Colors.Grey.Lighten3).Padding(2).Text(string.Empty);
                table.Cell().BorderLeft(0.5f).BorderRight(1).BorderTop(1).BorderBottom(0.5f)
                    .Background(Colors.Grey.Lighten3).Padding(2)
                    .AlignRight().Text(dept.TotalValue.ToString("N2")).Bold().FontSize(8);
            }

            var grandQty = data.Sum(d => d.TotalItemsIssued);
            var grandAmt = data.Sum(d => d.TotalValue);
            table.Cell().ColumnSpan(4).BorderLeft(1).BorderRight(0.5f).BorderTop(1.5f).BorderBottom(1)
                .Background(Colors.Grey.Lighten2).Padding(2)
                .AlignRight().Text("GRAND TOTAL").Bold().FontSize(9);
            table.Cell().BorderLeft(0.5f).BorderRight(0.5f).BorderTop(1.5f).BorderBottom(1)
                .Background(Colors.Grey.Lighten2).Padding(2)
                .AlignRight().Text(grandQty.ToString()).Bold().FontSize(9);
            table.Cell().BorderLeft(0.5f).BorderRight(0.5f).BorderTop(1.5f).BorderBottom(1)
                .Background(Colors.Grey.Lighten2).Padding(2).Text(string.Empty);
            table.Cell().BorderLeft(0.5f).BorderRight(1).BorderTop(1.5f).BorderBottom(1)
                .Background(Colors.Grey.Lighten2).Padding(2)
                .AlignRight().Text(grandAmt.ToString("N2")).Bold().FontSize(9);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        if (signatories.Count == 0)
        {
            container.AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(8);
                x.CurrentPageNumber().FontSize(8);
                x.Span(" of ").FontSize(8);
                x.TotalPages().FontSize(8);
            });
            return;
        }

        container.Column(col =>
        {
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    foreach (var _ in signatories)
                        c.RelativeColumn();
                });
                foreach (var sig in signatories)
                {
                    table.Cell().Padding(4).Column(inner =>
                    {
                        inner.Item().Text(sig.Label).Bold().FontSize(7).AlignCenter();
                        inner.Item().PaddingTop(10).LineHorizontal(0.5f);
                        inner.Item().Text(sig.Name).Bold().FontSize(8).AlignCenter();
                        inner.Item().Text(sig.Title).FontSize(7).AlignCenter();
                    });
                }
            });

            col.Item().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }

    private string BuildSerialNo()
    {
        var now = DateTimeOffset.UtcNow;
        return $"{now.Year}-{now.Month:D2}-{data.Count:D3}";
    }

    private string BuildDateRange()
    {
        if (from.HasValue && to.HasValue) return $"{from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
        if (from.HasValue) return $"From {from.Value:yyyy-MM-dd}";
        if (to.HasValue) return $"As of {to.Value:yyyy-MM-dd}";
        return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
    }
}
