using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Expendable.Features.v1.Reports.GenerateDepartmentIssuancePdf;

internal sealed class DepartmentIssuancePdfDocument : IDocument
{
    private readonly List<DepartmentIssuanceSummaryDto> _data;
    private readonly OrganizationProfileDto? _org;
    private readonly List<ReportSignatoryDto> _signatories;
    private readonly DateTimeOffset? _from;
    private readonly DateTimeOffset? _to;
    private readonly Dictionary<string, string> _departmentNames;

    public DepartmentIssuancePdfDocument(
        List<DepartmentIssuanceSummaryDto> data,
        OrganizationProfileDto? org,
        List<ReportSignatoryDto> signatories,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Dictionary<string, string> departmentNames)
    {
        _data = data;
        _org = org;
        _signatories = signatories;
        _from = from;
        _to = to;
        _departmentNames = departmentNames;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = "Report of Supplies and Materials Issued",
        Author = _org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
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
            if (_org is not null)
            {
                col.Item().AlignCenter().Text(_org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(_org.Address))
                    col.Item().AlignCenter().Text(_org.Address).FontSize(8);
            }

            col.Item().PaddingTop(6).AlignCenter()
                .Text("REPORT OF SUPPLIES AND MATERIALS ISSUED")
                .Bold().FontSize(11).LetterSpacing(1);

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                    c.RelativeColumn();
                });

                table.Cell().Text("Entity Name:").Bold();
                table.Cell().Text(_org?.Name ?? string.Empty);
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
                c.ConstantColumn(24);     // #
                c.RelativeColumn(3);      // Stock No.
                c.RelativeColumn(7);      // Description
                c.RelativeColumn(2);      // Unit
                c.ConstantColumn(40);     // Qty
                c.RelativeColumn(2.5f);   // Unit Cost
                c.RelativeColumn(2.5f);   // Amount
            });

            // Column headers
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
            foreach (var dept in _data)
            {
                var deptName = _departmentNames.TryGetValue(dept.DepartmentId, out var n) ? n : dept.DepartmentId;

                // Department header row
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

                // Subtotal row
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

            // Grand total row
            var grandQty = _data.Sum(d => d.TotalItemsIssued);
            var grandAmt = _data.Sum(d => d.TotalValue);
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
        if (_signatories.Count == 0)
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
                    foreach (var _ in _signatories)
                        c.RelativeColumn();
                });

                foreach (var sig in _signatories)
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
        return $"{now.Year}-{now.Month:D2}-{_data.Count:D3}";
    }

    private string BuildDateRange()
    {
        if (_from.HasValue && _to.HasValue)
            return $"{_from.Value:yyyy-MM-dd} to {_to.Value:yyyy-MM-dd}";
        if (_from.HasValue)
            return $"From {_from.Value:yyyy-MM-dd}";
        if (_to.HasValue)
            return $"As of {_to.Value:yyyy-MM-dd}";
        return DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
    }
}
