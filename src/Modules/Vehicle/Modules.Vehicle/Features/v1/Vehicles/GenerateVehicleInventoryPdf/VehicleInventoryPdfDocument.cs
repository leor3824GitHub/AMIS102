using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FSH.Modules.MasterData.Contracts.v1.ReportSignatories;
using FSH.Modules.Vehicle.Contracts.v1.Vehicles;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Vehicle.Features.v1.Vehicles.GenerateVehicleInventoryPdf;

internal sealed class VehicleInventoryPdfDocument : IDocument
{
    private readonly List<MotorVehicleInventoryItemDto> _items;
    private readonly OrganizationProfileDto? _org;
    private readonly List<ReportSignatoryDto> _signatories;
    private readonly DateTime? _asOfDate;

    public VehicleInventoryPdfDocument(
        List<MotorVehicleInventoryItemDto> items,
        OrganizationProfileDto? org,
        List<ReportSignatoryDto> signatories,
        DateTime? asOfDate)
    {
        _items = items;
        _org = org;
        _signatories = signatories;
        _asOfDate = asOfDate;
    }

    public DocumentMetadata GetMetadata() => new DocumentMetadata
    {
        Title = "Inventory of Motor Vehicles",
        Author = _org?.Name ?? string.Empty
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
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
            col.Item().AlignCenter().Text("Republic of the Philippines").FontSize(9);
            if (_org is not null)
            {
                col.Item().AlignCenter().Text(_org.Name.ToUpperInvariant()).Bold().FontSize(11);
                if (!string.IsNullOrWhiteSpace(_org.Address))
                    col.Item().AlignCenter().Text(_org.Address).FontSize(8);
            }
            col.Item().PaddingTop(6).AlignCenter().Text("INVENTORY OF MOTOR VEHICLES")
                .Bold().FontSize(11);
            col.Item().AlignCenter()
                .Text($"As of {(_asOfDate ?? DateTime.Today):MMMM d, yyyy}").FontSize(9);
            col.Item().PaddingTop(4).LineHorizontal(1);
        });
    }

    private void ComposeBody(IContainer container)
    {
        container.PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);   // QTY
                c.RelativeColumn(5);    // Description (multi-line)
                c.ConstantColumn(58);   // Plate No.
                c.ConstantColumn(62);   // Vehicle Use
                c.ConstantColumn(32);   // No. Cyl
                c.ConstantColumn(55);   // Engine CC
                c.ConstantColumn(46);   // Fuel Type
                c.ConstantColumn(34);   // Year
                c.RelativeColumn(2);    // Cost
                c.RelativeColumn(3);    // Running Condition
                c.RelativeColumn(3);    // Accountable Officer
            });

            var hStyle = TextStyle.Default.Bold().FontSize(7);
            var specBg = Colors.Blue.Lighten4;
            var acqBg = Colors.Green.Lighten4;

            table.Header(h =>
            {
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("QTY").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("DESCRIPTION\n(Make, Model, Motor No.,\nChassis No., Classification)").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("PLATE\nNUMBER").Style(hStyle);
                h.Cell().ColumnSpan(3).Border(1).Padding(2).AlignCenter().Background(specBg).Text("SPECIFICATION").Style(hStyle);
                h.Cell().ColumnSpan(3).Border(1).Padding(2).AlignCenter().Background(acqBg).Text("ACQUISITION").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("RUNNING\nCONDITION").Style(hStyle);
                h.Cell().RowSpan(2).Border(1).Padding(2).AlignCenter().Text("ACCOUNTABLE\nOFFICER").Style(hStyle);

                // 2nd row
                h.Cell().Border(1).Padding(2).AlignCenter().Background(specBg).Text("VEHICLE\nTYPE/USE").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Background(specBg).Text("NO.\nCYL").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Background(specBg).Text("ENGINE\nDISP. (CC)").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Background(acqBg).Text("FUEL\nTYPE").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Background(acqBg).Text("YEAR").Style(hStyle);
                h.Cell().Border(1).Padding(2).AlignCenter().Background(acqBg).Text("COST").Style(hStyle);
            });

            foreach (var v in _items)
            {
                var description = BuildDescription(v);
                var officerText = BuildOfficer(v);

                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.Qty.ToString()).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(description).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.PlateNumber).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.VehicleUse ?? string.Empty).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.NumberOfCylinders?.ToString() ?? "—").FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.EngineDisplacementCC?.ToString() ?? "—").FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.FuelType ?? "—").FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.Year.ToString()).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignRight()
                    .Text(v.AcquisitionCost.HasValue ? v.AcquisitionCost.Value.ToString("N2") : "—").FontSize(7);
                table.Cell().Border(0.5f).Padding(2).AlignCenter().Text(v.RunningCondition).FontSize(7);
                table.Cell().Border(0.5f).Padding(2).Text(officerText).FontSize(7);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            if (_signatories.Count > 0)
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
            }

            col.Item().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(7);
                x.CurrentPageNumber().FontSize(7);
                x.Span(" of ").FontSize(7);
                x.TotalPages().FontSize(7);
            });
        });
    }

    private static string BuildDescription(MotorVehicleInventoryItemDto v)
    {
        var parts = new List<string> { v.Description.ToUpperInvariant() };
        if (!string.IsNullOrWhiteSpace(v.MotorNumber)) parts.Add($"MOTOR NO. {v.MotorNumber}");
        if (!string.IsNullOrWhiteSpace(v.ChassisNumber)) parts.Add($"CHASSIS NO. {v.ChassisNumber}");
        if (!string.IsNullOrWhiteSpace(v.VehicleClassification)) parts.Add(v.VehicleClassification.ToUpperInvariant());
        return string.Join("\n", parts);
    }

    private static string BuildOfficer(MotorVehicleInventoryItemDto v)
    {
        if (string.IsNullOrWhiteSpace(v.AccountableOfficer)) return string.Empty;
        return string.IsNullOrWhiteSpace(v.AccountableOfficerTitle)
            ? v.AccountableOfficer
            : $"{v.AccountableOfficer}\n{v.AccountableOfficerTitle}";
    }
}
