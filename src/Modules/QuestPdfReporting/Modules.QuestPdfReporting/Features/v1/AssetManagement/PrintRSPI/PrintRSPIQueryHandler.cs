using System.Globalization;
using AMIS.Modules.AssetManagement.Contracts.v1.Reports;
using AMIS.Modules.QuestPdfReporting.Services;
using Mediator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRSPI;

public sealed class PrintRSPIQueryHandler(IMediator mediator)
    : IQueryHandler<PrintRSPIQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(PrintRSPIQuery query, CancellationToken ct)
    {
        var report = await mediator.Send(
            new GetRSPIQuery(
                query.DateFrom,
                query.DateTo,
                query.AssetType,
                query.ActiveOnly,
                query.PageNumber,
                query.PageSize),
            ct).ConfigureAwait(false);

        var generatedAt = DateTimeOffset.UtcNow;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                QuestPdfPaperSize.Apply(page, query.PaperSize, query.Orientation);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Report of Semi-Expendable Property Issued (RSPI)").Bold().FontSize(14);
                    col.Item().Text($"Period: {query.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(any)"} to {query.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(any)"}");
                    col.Item().Text($"Active only: {(query.ActiveOnly ? "Yes" : "No")}    Asset type: {query.AssetType?.ToString() ?? "All"}");
                    col.Item().Text($"Generated: {generatedAt.ToString("u", CultureInfo.InvariantCulture)}");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Text($"Lines: {report.TotalCount}    Amount: {report.OverallAmountTotal.ToString("N2", CultureInfo.InvariantCulture)}").SemiBold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.0f);
                            columns.RelativeColumn(2.8f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(2.0f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.5f);
                        });

                        static IContainer HeaderCell(IContainer cell) => cell
                            .Background(Colors.Grey.Lighten2)
                            .Padding(4)
                            .BorderBottom(1);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Property / Code").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Item").SemiBold();
                            header.Cell().Element(HeaderCell).Text("ICS No").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Date").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Received By").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Type").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit Cost").SemiBold();
                        });

                        foreach (var item in report.Items)
                        {
                            table.Cell().Padding(4).BorderBottom(0.5f).Text($"{item.PropertyNo}\n{item.ItemCode}");
                            table.Cell().Padding(4).BorderBottom(0.5f).Text(item.ItemName);
                            table.Cell().Padding(4).BorderBottom(0.5f).Text(item.ICSNo);
                            table.Cell().Padding(4).BorderBottom(0.5f).Text(item.ICSDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                            table.Cell().Padding(4).BorderBottom(0.5f).Text(item.ReceivedByEmployeeName);
                            table.Cell().Padding(4).BorderBottom(0.5f).Text(item.AssetType);
                            table.Cell().Padding(4).BorderBottom(0.5f).AlignRight().Text(item.UnitCost.ToString("N2", CultureInfo.InvariantCulture));
                        }
                    });

                    if (report.Signatories.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Signatories").SemiBold();
                        foreach (var signatory in report.Signatories.OrderBy(x => x.SortOrder))
                            col.Item().Text($"{signatory.Label}: {signatory.Name} ({signatory.Title})");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
