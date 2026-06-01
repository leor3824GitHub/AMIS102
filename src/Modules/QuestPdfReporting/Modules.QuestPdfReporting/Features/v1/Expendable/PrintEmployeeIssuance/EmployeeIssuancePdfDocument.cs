using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;

internal sealed class EmployeeIssuancePdfDocument(
    List<EmployeeIssuanceDto>   records,
    OrganizationProfileDto?     org,
    DateTimeOffset?             from,
    DateTimeOffset?             to,
    Dictionary<string, string>  employeeNames,
    Dictionary<string, string>  departmentNames,
    string                      paperSize   = "a4",
    string                      orientation = "landscape",
    float                       marginMm    = 15f) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title  = "Employee Issuance History",
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
            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
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
            col.Item().PaddingTop(6).AlignCenter().Text("EMPLOYEE ISSUANCE HISTORY").Bold().FontSize(11);

            if (from.HasValue || to.HasValue)
                col.Item().AlignCenter().Text(BuildDateRange()).FontSize(9);

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(3);
                c.RelativeColumn(3);
                c.RelativeColumn(2);
                c.ConstantColumn(32);
                c.RelativeColumn(2);
            });

            var hStyle = TextStyle.Default.Bold().FontSize(8);
            table.Header(h =>
            {
                h.Cell().Border(1).Padding(2).Text("Request No.").Style(hStyle);
                h.Cell().Border(1).Padding(2).Text("Employee").Style(hStyle);
                h.Cell().Border(1).Padding(2).Text("Department").Style(hStyle);
                h.Cell().Border(1).Padding(2).Text("Fulfilled On").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Text("Items").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignRight().Text("Total Value").Style(hStyle);
            });

            foreach (var record in records)
            {
                var employeeName = employeeNames.TryGetValue(record.EmployeeId, out var en) ? en : record.EmployeeId;
                var deptName = departmentNames.TryGetValue(record.DepartmentId, out var dn) ? dn : record.DepartmentId;

                table.Cell().Border(0.5f).Padding(2).Text(record.RequestNumber).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(employeeName).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(deptName).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter()
                    .Text(record.FulfilledOnUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(record.Items.Count.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text($"₱{record.TotalValue:N2}").FontSize(8);

                foreach (var item in record.Items)
                {
                    table.Cell().Border(0.5f).Padding(2).PaddingLeft(12)
                        .Text($"• {item.ProductCode}").FontSize(7).Italic();
                    table.Cell().ColumnSpan(2).Border(0.5f).Padding(2)
                        .Text(item.ProductName).FontSize(7);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter()
                        .Text($"Qty: {item.QuantityIssued}").FontSize(7);
                    table.Cell().Border(0.5f).Padding(2).AlignCenter()
                        .Text($"@₱{item.UnitPrice:N4}").FontSize(7);
                    table.Cell().Border(0.5f).Padding(2).AlignRight()
                        .Text($"₱{item.TotalValue:N2}").FontSize(7);
                }
            }
        });
    }

    private string BuildDateRange()
    {
        if (from.HasValue && to.HasValue)
            return $"Period: {from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
        if (from.HasValue) return $"From: {from.Value:yyyy-MM-dd}";
        if (to.HasValue) return $"As of: {to.Value:yyyy-MM-dd}";
        return string.Empty;
    }
}
