using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintPropertySticker;
using QuestPDF.Companion;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Shouldly;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.AssetRegister.PrintPropertySticker;

public sealed class PropertyStickerPdfDocumentTests
{
    static PropertyStickerPdfDocumentTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void Generates_NonEmpty_Pdf_With_Qr()
    {
        var doc = new PropertyStickerPdfDocument(SampleStickers(), SampleOrg());

        var bytes = doc.GeneratePdf();

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(0);
    }

    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_PropertyStickers_InCompanion()
    {
        var doc = new PropertyStickerPdfDocument(SampleStickers(), SampleOrg());
        doc.ShowInCompanion();
    }

    // 12 stickers exercises the 2×5 grid pagination (page 1 = 10, page 2 = 2).
    private static List<PropertyStickerModel> SampleStickers()
    {
        var list = new List<PropertyStickerModel>();
        for (var i = 1; i <= 12; i++)
        {
            var ppe = i % 2 == 1;
            list.Add(new PropertyStickerModel(
                PropertyNo: ppe ? $"2020-NFA-MSD-DP-B-{3397 + i:D4}" : $"2021-NFA-MSD-OF-C-{182 + i:D4}",
                Description: ppe
                    ? "Desktop-ACER Veriton X2665G, HDD-1TB, SSD-256GB, VRAM-4GB, RAM-DDR4 16GB OS-WIN10"
                    : "Office Chair, Executive, High-back",
                SerialNo: ppe ? null : $"OC-2021-{182 + i:D4}",
                AcquisitionDate: new DateOnly(2020 + (i % 3), ((i % 12) + 1), 15),
                UnitCost: ppe ? 42_390.40m : 6_750.00m,
                AssetType: ppe ? AssetType.PPE : AssetType.SE,
                AccountableOfficer: "Lorena G. Dandan",
                Location: "Room 201, MSD Bldg."));
        }

        return list;
    }

    private static OrganizationProfileDto SampleOrg() =>
        new(
            Id: Guid.NewGuid(),
            Name: "Caraga Regional Office",
            ShortName: "NFA Caraga",
            Address: "J. Rosales Ave. Butuan City",
            LogoUrl: null,
            AnnexECode: null,
            PropertyCustodianName: "ROEL D. CAPERIG",
            PropertyCustodianDesignation: "PMO IV");
}
