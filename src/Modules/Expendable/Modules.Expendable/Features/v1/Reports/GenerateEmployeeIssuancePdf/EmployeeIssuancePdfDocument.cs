using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Expendable.Features.v1.Reports.GenerateEmployeeIssuancePdf;

internal sealed class EmployeeIssuancePdfDocument : IDocument
{
    private readonly List<EmployeeIssuanceDto> _records;
    private readonly OrganizationProfileDto? _org;
    private readonly DateTimeOffset? _from;
    private readonly DateTimeOffset? _to;
    private readonly Dictionary<string, string> _employeeNames;
    private readonly Dictionary<string, string> _departmentNames;

    public EmployeeIssuancePdfDocument(
        List<EmployeeIssuanceDto> records,
        OrganizationProfileDto? org,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Dictionary<string, string> employeeNames,
        Dictionary<string, string> departmentNames)
    {
        _records = records;
        _org = org;
        _from = from;
        _to = to;
        _employeeNames = employeeNames;
        _departmentNames = departmentNames;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = "Employee Issuance History",
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
            if (_org is not null)
            {
                col.Item().AlignCenter().Text(_org.Name).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(_org.Address))
                    col.Item().AlignCenter().Text(_org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text("EMPLOYEE ISSUANCE HISTORY").Bold().FontSize(11);

            if (_from.HasValue || _to.HasValue)
            {
                var range = BuildDateRange();
                col.Item().AlignCenter().Text(range).FontSize(9);
            }

            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);    // Request No.
                c.RelativeColumn(3);    // Employee
                c.RelativeColumn(3);    // Department
                c.RelativeColumn(2);    // Fulfilled On
                c.ConstantColumn(32);   // Items
                c.RelativeColumn(2);    // Total Value
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

            foreach (var record in _records)
            {
                var employeeName = _employeeNames.TryGetValue(record.EmployeeId, out var en) ? en : record.EmployeeId;
                var deptName = _departmentNames.TryGetValue(record.DepartmentId, out var dn) ? dn : record.DepartmentId;

                table.Cell().Border(0.5f).Padding(2).Text(record.RequestNumber).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(employeeName).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).Text(deptName).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter()
                    .Text(record.FulfilledOnUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(record.Items.Count.ToString()).FontSize(8);
                table.Cell().Border(0.5f).Padding(2).AlignRight().Text($"₱{record.TotalValue:N2}").FontSize(8);

                // Detail rows — one per item
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
        if (_from.HasValue && _to.HasValue)
            return $"Period: {_from.Value:yyyy-MM-dd} to {_to.Value:yyyy-MM-dd}";
        if (_from.HasValue) return $"From: {_from.Value:yyyy-MM-dd}";
        if (_to.HasValue) return $"As of: {_to.Value:yyyy-MM-dd}";
        return string.Empty;
    }
}
