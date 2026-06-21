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
    private const string Blue  = "#1F3C88";
    private const string Red   = "#C81E1E";
    private const string Ink   = "#1A1A1A";   // value text
    private const string Slate = "#5B6B8C";   // de-emphasised (address, designation)
    private const float  SheetMarginMm = 8f;
    private const float  CellGap = 4f;
    // Inner padding of each property box (asymmetric: more space at the top, a larger gap at the bottom).
    private const float  BoxPadTop    = 9f;
    private const float  BoxPadBottom = 13f;
    private const float  BoxPadSide   = 4f;
    private const float  LabelColWidth = 80f; // fixed label column → values align on one edge

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
        // Small per-row slack keeps the 5 rows just under the exact page height so sub-pixel
        // rounding can't spill the last row onto a new page.
        var rowHeightMm = ((pageHeightMm - (2 * SheetMarginMm)) / Math.Max(1, rows)) - 0.4f;

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
            .PaddingHorizontal(BoxPadSide)
            .PaddingTop(BoxPadTop)                 // more space above the header
            .PaddingBottom(BoxPadBottom)           // larger gap below the signature
            .Column(col =>
            {
                col.Item().Element(ComposeHeader);
                col.Item().PaddingTop(2).LineHorizontal(0.6f).LineColor(Blue);
                col.Item().PaddingTop(3).Element(c => ComposeBody(c, m));
                col.Item().PaddingTop(3).LineHorizontal(0.6f).LineColor(Blue);   // line after Type
                col.Item().PaddingTop(8).Element(ComposeSignature);             // signing space above the name
            });
    }

    private void ComposeHeader(IContainer container)
    {
        // Logo + agency text as a single centred group (keeps the header compact).
        container.AlignCenter().Row(row =>
        {
            var logo = LogoBytes.Value;
            if (logo is not null)
                row.AutoItem().AlignMiddle().Width(24).Height(24).Image(logo).FitArea();

            row.AutoItem().PaddingLeft(6).AlignMiddle().Column(col =>
            {
                col.Item().Text("National Food Authority").Bold().FontSize(9.5f).FontColor(Blue);
                col.Item().Text(string.IsNullOrWhiteSpace(org?.Name) ? "Caraga Regional Office" : org!.Name)
                    .Bold().FontSize(7f).FontColor(Blue);
                col.Item().Text(string.IsNullOrWhiteSpace(org?.Address) ? "J. Rosales Ave., Butuan City" : org!.Address!)
                    .FontSize(5.5f).FontColor(Slate);
            });
        });
    }

    // Body: labelled fields on the left, the QR block (property number on top, QR below) on the right.
    private static void ComposeBody(IContainer container, PropertyStickerModel m)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => ComposeFields(c, m));
            row.ConstantItem(66).PaddingLeft(6).AlignMiddle().Element(c => ComposeQrBlock(c, m));
        });
    }

    private static void ComposeFields(IContainer container, PropertyStickerModel m)
    {
        container.Column(col =>
        {
            col.Spacing(1.5f);

            LabeledField(col, "Item:", Dash(m.Description));
            LabeledField(col, "Serial No:", Dash(m.SerialNo));
            LabeledField(col, "Date Acquired:", m.AcquisitionDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
            LabeledField(col, "Value:", "Php " + m.UnitCost.ToString("N2", CultureInfo.InvariantCulture));
            LabeledField(col, "Accountable Officer:", Dash(m.AccountableOfficer));
            LabeledField(col, "Location:", Dash(m.Location));

            col.Item().PaddingTop(1).Element(c => ComposeTypeRow(c, m));
        });
    }

    private static void ComposeQrBlock(IContainer container, PropertyStickerModel m)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(m.PropertyNo).Bold().FontSize(5.5f).FontColor(Ink);
            col.Item().PaddingTop(2).AlignCenter().DrawQr(m.PropertyNo, 52f);
        });
    }

    private static void ComposeTypeRow(IContainer container, PropertyStickerModel m)
    {
        container.Row(row =>
        {
            row.ConstantItem(LabelColWidth).AlignMiddle().Text("Type").SemiBold().FontColor(Blue).FontSize(7);
            row.AutoItem().AlignMiddle().Element(c => Checkbox(c, m.AssetType == AssetType.PPE));
            row.AutoItem().AlignMiddle().PaddingLeft(3).PaddingRight(16).Text("FA").FontSize(7).FontColor(Ink);
            row.AutoItem().AlignMiddle().Element(c => Checkbox(c, m.AssetType == AssetType.SE));
            row.AutoItem().AlignMiddle().PaddingLeft(3).Text("SE").FontSize(7).FontColor(Ink);
        });
    }

    private void ComposeSignature(IContainer container) =>
        container.AlignCenter().Column(col =>
        {
            col.Item().AlignCenter().Text(CustodianName).Bold().FontSize(8f).FontColor(Blue);
            col.Item().AlignCenter().Text(CustodianDesignation).FontSize(6f).FontColor(Slate);
        });

    // ── Field helpers ────────────────────────────────────────────────────────

    private static void LabeledField(ColumnDescriptor col, string label, string value) =>
        col.Item().Element(c => InlineLabeled(c, label, LabelColWidth, value));

    private static void InlineLabeled(IContainer container, string label, float labelWidth, string value)
    {
        container.Row(row =>
        {
            row.ConstantItem(labelWidth).AlignTop().Text(label).SemiBold().FontColor(Blue).FontSize(7);
            row.RelativeItem().AlignTop().Text(value).FontSize(7).FontColor(Ink);
        });
    }

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

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
