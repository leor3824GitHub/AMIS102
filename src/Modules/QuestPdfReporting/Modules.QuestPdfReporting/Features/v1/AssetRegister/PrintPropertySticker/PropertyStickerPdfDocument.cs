using System.Globalization;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;

/// <summary>
/// Printable NFA property stickers laid out as a label sheet — a fixed <c>Columns × Rows</c> grid per
/// page (default 2 × 5 = 10 per sheet), paginating as needed. Each cell is a compact sticker carrying
/// a QR code of the Property No. and the agency property custodian (from the MasterData Organization
/// Profile) as the bottom-right signatory. Sourced from a single asset (reprint) or every ICS/PAR line.
/// </summary>
internal sealed class PropertyStickerPdfDocument(
    IReadOnlyList<PropertyStickerModel> stickers,
    OrganizationProfileDto?             org,
    string                              paperSize   = "longbond",
    string                              orientation = "portrait",
    int                                 columns     = 2,
    int                                 rows        = 5) : IDocument
{
    private const string Blue = "#1F3C88";
    private const string Red  = "#C81E1E";
    private const float  SheetMarginMm = 8f;
    private const float  CellGap = 4f;

    private const string DefaultCustodianName        = "ROEL D. CAPERIG";
    private const string DefaultCustodianDesignation = "PMO IV";

    private int PerPage => Math.Max(1, columns) * Math.Max(1, rows);

    private string CustodianName =>
        string.IsNullOrWhiteSpace(org?.PropertyCustodianName) ? DefaultCustodianName : org!.PropertyCustodianName!;

    private string CustodianDesignation =>
        string.IsNullOrWhiteSpace(org?.PropertyCustodianDesignation) ? DefaultCustodianDesignation : org!.PropertyCustodianDesignation!;

    public DocumentMetadata GetMetadata() => new()
    {
        Title  = stickers.Count == 1
            ? $"Property Sticker — {stickers[0].PropertyNo}"
            : $"Property Stickers ({stickers.Count})",
        Author = org?.Name ?? "National Food Authority",
    };

    public void Compose(IDocumentContainer container)
    {
        if (stickers.Count == 0)
        {
            container.Page(page =>
            {
                QuestPdfPaperSize.Apply(page, paperSize, orientation, SheetMarginMm);
                page.DefaultTextStyle(x => x.FontFamily("Arial"));
                page.Content().AlignCenter().AlignMiddle().Text("No items to print.").Italic();
            });
            return;
        }

        for (var start = 0; start < stickers.Count; start += PerPage)
        {
            var pageStickers = stickers.Skip(start).Take(PerPage).ToList();
            container.Page(page =>
            {
                QuestPdfPaperSize.Apply(page, paperSize, orientation, SheetMarginMm);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(6));
                page.Content().Element(c => ComposeSheet(c, pageStickers));
            });
        }
    }

    private void ComposeSheet(IContainer container, List<PropertyStickerModel> pageStickers)
    {
        var (longMm, shortMm) = QuestPdfPaperSize.Resolve(paperSize);
        var pageHeightMm = QuestPdfPaperSize.IsLandscape(orientation) ? shortMm : longMm;
        var rowHeightMm = (pageHeightMm - (2 * SheetMarginMm)) / Math.Max(1, rows);

        container.Column(col =>
        {
            for (var r = 0; r < rows; r++)
            {
                col.Item().Height(rowHeightMm, Unit.Millimetre).Row(row =>
                {
                    for (var c = 0; c < columns; c++)
                    {
                        var cell = row.RelativeItem();
                        var index = (r * columns) + c;
                        if (index < pageStickers.Count)
                            cell.Padding(CellGap).Element(x => ComposeStickerCell(x, pageStickers[index]));
                    }
                });
            }
        });
    }

    private void ComposeStickerCell(IContainer container, PropertyStickerModel m)
    {
        container
            .Border(1.2f).BorderColor(Red)        // outer red frame
            .Padding(1.5f)
            .Border(0.6f).BorderColor(Blue)       // inner blue frame
            .Padding(4)
            .Column(col =>
            {
                col.Item().Element(ComposeHeader);
                col.Item().PaddingTop(2).Element(c => ComposeFields(c, m));
                col.Item().PaddingTop(3).Element(c => ComposeFooter(c, m));
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            var logo = LogoBytes.Value;
            if (logo is not null)
                row.ConstantItem(28).AlignMiddle().Height(24).Image(logo).FitArea();
            else
                row.ConstantItem(28);

            row.RelativeItem().AlignMiddle().Column(col =>
            {
                col.Item().Text("National Food Authority").Bold().FontSize(8).FontColor(Blue);
                col.Item().Text(string.IsNullOrWhiteSpace(org?.Name) ? "Caraga Regional Office" : org!.Name)
                    .Bold().FontSize(6.5f).FontColor(Blue);
                col.Item().Text(string.IsNullOrWhiteSpace(org?.Address) ? "J. Rosales Ave. Butuan City" : org!.Address!)
                    .FontSize(5.5f).FontColor(Blue);
            });
        });
    }

    private static void ComposeFields(IContainer container, PropertyStickerModel m)
    {
        container.Column(col =>
        {
            col.Spacing(1.5f);

            Field(col, "Item:", m.Description);
            Field(col, "Serial No:", m.SerialNo);
            Field(col, "Property Code:", m.PropertyNo);

            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => InlineField(c, "Date Acquired:", m.AcquisitionDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)));
                row.ConstantItem(6);
                row.RelativeItem(2).Element(c => InlineField(c, "Value: Php", m.UnitCost.ToString("N2", CultureInfo.InvariantCulture)));
            });

            Field(col, "Accountable Officer:", m.AccountableOfficer);
            Field(col, "Location:", m.Location);

            col.Item().PaddingTop(1).Element(c => ComposeTypeRow(c, m));
        });
    }

    private static void ComposeTypeRow(IContainer container, PropertyStickerModel m)
    {
        container.Row(row =>
        {
            row.AutoItem().AlignMiddle().Text("Type:").SemiBold().FontColor(Blue);
            row.ConstantItem(8);
            row.AutoItem().AlignMiddle().Element(c => Checkbox(c, m.AssetType == AssetType.PPE));
            row.AutoItem().AlignMiddle().PaddingLeft(2).PaddingRight(10).Text("FA");
            row.AutoItem().AlignMiddle().Element(c => Checkbox(c, m.AssetType == AssetType.SE));
            row.AutoItem().AlignMiddle().PaddingLeft(2).Text("SE");
        });
    }

    private void ComposeFooter(IContainer container, PropertyStickerModel m)
    {
        container.PaddingTop(2).Row(row =>
        {
            // QR code (encodes the Property No.) anchored bottom-left.
            row.ConstantItem(48).AlignBottom().Column(col =>
            {
                col.Item().AlignLeft().DrawQr(m.PropertyNo, 44f);
            });

            // Property custodian signatory (from Organization Profile) anchored bottom-right.
            row.RelativeItem().AlignBottom().Column(col =>
            {
                col.Item().AlignRight().Text(CustodianName).Bold().FontSize(6.5f).FontColor(Blue);
                col.Item().AlignRight().Text(CustodianDesignation).FontSize(5.5f).FontColor(Blue);
            });
        });
    }

    // ── Field helpers ────────────────────────────────────────────────────────

    private static void Field(ColumnDescriptor col, string label, string? value) =>
        col.Item().Element(c => InlineField(c, label, value));

    private static void InlineField(IContainer container, string label, string? value)
    {
        container.Row(row =>
        {
            row.AutoItem().AlignTop().Text(label).SemiBold().FontColor(Blue);
            row.RelativeItem().PaddingLeft(2).BorderBottom(0.6f).BorderColor(Blue)
                .AlignTop().Text(value ?? string.Empty);
        });
    }

    private static void Checkbox(IContainer container, bool isChecked) =>
        container.Width(10).Height(10).Border(0.7f).BorderColor(Red)
            .AlignCenter().AlignMiddle()
            .Text(isChecked ? "X" : string.Empty).Bold().FontColor(Red).FontSize(6);

    // ── Bundled agency logo (auto-embedded from ReportAssets\**\*.png) ────────

    private static readonly Lazy<byte[]?> LogoBytes = new(LoadLogo);

    private static byte[]? LoadLogo()
    {
        var asm = typeof(PropertyStickerPdfDocument).Assembly;
        var images = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".ReportAssets.", StringComparison.OrdinalIgnoreCase)
                && (n.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var name = images.FirstOrDefault(n => n.Contains("logo", StringComparison.OrdinalIgnoreCase))
                   ?? images.FirstOrDefault();
        if (name is null)
            return null;

        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null)
            return null;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
