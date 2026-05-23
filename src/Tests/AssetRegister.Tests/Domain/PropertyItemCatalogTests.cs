using AMIS.Modules.AssetRegister.Contracts.v1.Catalog;
using AMIS.Modules.AssetRegister.Domain.Catalog;
using Shouldly;
using Xunit;

namespace AssetRegister.Tests.Domain;

public sealed class PropertyItemCatalogTests
{
    [Fact]
    public void Create_WithUacs_StartsReady()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-001", "Desktop Computer", "OE", "OEC", "piece",
            uacsObjectCode: "1-07-05-030", estimatedUsefulLifeYears: 5);

        c.Status.ShouldBe(CatalogItemStatus.Ready);
        c.UacsObjectCode.ShouldBe("1-07-05-030");
    }

    [Fact]
    public void Create_WithoutUacs_StartsDraft()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-002", "Desktop Computer", "OE", "OEC", "piece",
            uacsObjectCode: null, estimatedUsefulLifeYears: 5);

        c.Status.ShouldBe(CatalogItemStatus.Draft);
        c.UacsObjectCode.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyUacs_StartsDraft()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-003", "Desktop Computer", "OE", "OEC", "piece",
            uacsObjectCode: "   ", estimatedUsefulLifeYears: 5);

        c.Status.ShouldBe(CatalogItemStatus.Draft);
    }

    [Fact]
    public void BackfillUacs_OnDraft_PromotesToReady()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-004", "Desktop Computer", "OE", "OEC", "piece", null, 5);

        var promoted = c.BackfillUacs("1-07-05-030");

        promoted.ShouldBeTrue();
        c.Status.ShouldBe(CatalogItemStatus.Ready);
        c.UacsObjectCode.ShouldBe("1-07-05-030");
    }

    [Fact]
    public void BackfillUacs_OnReady_IsNoop()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-005", "Desktop Computer", "OE", "OEC", "piece",
            uacsObjectCode: "1-07-05-030", estimatedUsefulLifeYears: 5);

        var promoted = c.BackfillUacs("999-99-99-999");

        promoted.ShouldBeFalse();
        c.UacsObjectCode.ShouldBe("1-07-05-030"); // existing wins
    }

    [Fact]
    public void BackfillUacs_WithEmptyValue_IsNoop()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-006", "Desktop Computer", "OE", "OEC", "piece", null, 5);

        var promoted = c.BackfillUacs("   ");

        promoted.ShouldBeFalse();
        c.Status.ShouldBe(CatalogItemStatus.Draft);
    }

    [Fact]
    public void Update_AddingUacs_PromotesDraftToReady()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-007", "Desktop Computer", "OE", "OEC", "piece", null, 5);

        c.Update("Updated Desc", "OE", "OEC", "piece",
            uacsObjectCode: "1-07-05-030", estimatedUsefulLifeYears: 5);

        c.Status.ShouldBe(CatalogItemStatus.Ready);
        c.UacsObjectCode.ShouldBe("1-07-05-030");
    }

    [Fact]
    public void Update_RemovingUacs_DoesNotDemote()
    {
        var c = PropertyItemCatalog.Create("t", "DPC-008", "Desktop Computer", "OE", "OEC", "piece",
            uacsObjectCode: "1-07-05-030", estimatedUsefulLifeYears: 5);

        c.Update("Updated Desc", "OE", "OEC", "piece",
            uacsObjectCode: null, estimatedUsefulLifeYears: 5);

        c.Status.ShouldBe(CatalogItemStatus.Ready); // we never demote
        c.UacsObjectCode.ShouldBeNull();
    }
}
