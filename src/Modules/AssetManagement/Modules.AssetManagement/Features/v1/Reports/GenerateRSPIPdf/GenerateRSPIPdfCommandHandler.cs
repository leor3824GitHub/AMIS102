using System.Globalization;
using AMIS.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;
using Mediator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.GenerateRSPIPdf;

public sealed class GenerateRSPIPdfCommandHandler(IMediator mediator)
    : ICommandHandler<GenerateRSPIPdfCommand, byte[]>
{
    public async ValueTask<byte[]> Handle(GenerateRSPIPdfCommand command, CancellationToken cancellationToken)
    {
        var report = await mediator.Send(
            new GetRSPIQuery(
                command.DateFrom,
                command.DateTo,
                command.AssetType,
                command.ActiveOnly,
                command.PageNumber,
                command.PageSize),
            cancellationToken);

        var generatedAt = DateTimeOffset.UtcNow;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Report of Semi-Expendable Property Issued (RSPI)").Bold().FontSize(14);
                    col.Item().Text($"Period: {command.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(any)"} to {command.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "(any)"}");
                    col.Item().Text($"Active only: {(command.ActiveOnly ? "Yes" : "No")}    Asset type: {command.AssetType?.ToString() ?? "All"}");
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
                            columns.RelativeColumn(2.0f); // Property / code
                            columns.RelativeColumn(2.8f); // Item name
                            columns.RelativeColumn(1.5f); // ICS
                            columns.RelativeColumn(1.3f); // Date
                            columns.RelativeColumn(2.0f); // Received by
                            columns.RelativeColumn(1.1f); // Type
                            columns.RelativeColumn(1.5f); // Unit cost
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
                        {
                            col.Item().Text($"{signatory.Label}: {signatory.Name} ({signatory.Title})");
                        }
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

