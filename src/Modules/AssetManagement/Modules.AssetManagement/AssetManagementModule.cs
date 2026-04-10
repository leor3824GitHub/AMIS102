using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.AssetManagement.Data;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.CreateICS;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSById;
using FSH.Modules.AssetManagement.Features.v1.InventoryCustodianSlips.GetICSList;
using FSH.Modules.AssetManagement.Features.v1.ReceivingReports.CreateSMRR;
using FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRById;
using FSH.Modules.AssetManagement.Features.v1.ReceivingReports.GetSMRRs;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.CreateSemiExpendableItem;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItemById;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.GetSemiExpendableItems;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableItems.UpdateSemiExpendableItem;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendableProperties;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.GetSemiExpendablePropertyById;
using FSH.Modules.AssetManagement.Features.v1.SemiExpendableProperties.RegisterSemiExpendableProperty;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.AssetManagement;

public class AssetManagementModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
    [
        new("View Semi-Expendable Items",   "View",   "AssetManagement.SemiExpendableItems",   IsBasic: true),
        new("Create Semi-Expendable Items", "Create", "AssetManagement.SemiExpendableItems"),
        new("Update Semi-Expendable Items", "Update", "AssetManagement.SemiExpendableItems"),
        new("Delete Semi-Expendable Items", "Delete", "AssetManagement.SemiExpendableItems"),

        new("View Semi-Expendable Properties",   "View",   "AssetManagement.SemiExpendableProperties",   IsBasic: true),
        new("Create Semi-Expendable Properties", "Create", "AssetManagement.SemiExpendableProperties"),
        new("Update Semi-Expendable Properties", "Update", "AssetManagement.SemiExpendableProperties"),
        new("Delete Semi-Expendable Properties", "Delete", "AssetManagement.SemiExpendableProperties"),

        new("View Receiving Reports",   "View",   "AssetManagement.ReceivingReports",   IsBasic: true),
        new("Create Receiving Reports", "Create", "AssetManagement.ReceivingReports"),
        new("Update Receiving Reports", "Update", "AssetManagement.ReceivingReports"),
        new("Delete Receiving Reports", "Delete", "AssetManagement.ReceivingReports"),

        new("View Inventory Custodian Slips",   "View",   "AssetManagement.InventoryCustodianSlips",   IsBasic: true),
        new("Create Inventory Custodian Slips", "Create", "AssetManagement.InventoryCustodianSlips"),
        new("Update Inventory Custodian Slips", "Update", "AssetManagement.InventoryCustodianSlips"),
        new("Delete Inventory Custodian Slips", "Delete", "AssetManagement.InventoryCustodianSlips"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(RegisteredPermissions);
        builder.Services.AddHeroDbContext<AssetManagementDbContext>();
        builder.Services.AddScoped<IDbInitializer, AssetManagementDbInitializer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/asset-management")
            .WithTags("AssetManagement")
            .WithApiVersionSet(apiVersionSet);

        var semiExpendableItemsGroup      = moduleGroup.MapGroup("/semi-expendable-items");
        var semiExpendablePropertiesGroup = moduleGroup.MapGroup("/semi-expendable-properties");
        var receivingReportsGroup         = moduleGroup.MapGroup("/receiving-reports");
        var icsGroup                      = moduleGroup.MapGroup("/inventory-custodian-slips");

        // Semi-Expendable Items
        CreateSemiExpendableItemEndpoint.Map(semiExpendableItemsGroup);
        GetSemiExpendableItemsEndpoint.Map(semiExpendableItemsGroup);
        GetSemiExpendableItemByIdEndpoint.Map(semiExpendableItemsGroup);
        UpdateSemiExpendableItemEndpoint.Map(semiExpendableItemsGroup);

        // Semi-Expendable Properties
        RegisterSemiExpendablePropertyEndpoint.Map(semiExpendablePropertiesGroup);
        GetSemiExpendablePropertiesEndpoint.Map(semiExpendablePropertiesGroup);
        GetSemiExpendablePropertyByIdEndpoint.Map(semiExpendablePropertiesGroup);

        // Receiving Reports (SMRR)
        CreateSMRREndpoint.Map(receivingReportsGroup);
        GetSMRRsEndpoint.Map(receivingReportsGroup);
        GetSMRRByIdEndpoint.Map(receivingReportsGroup);

        // Inventory Custodian Slips (ICS)
        CreateICSEndpoint.Map(icsGroup);
        GetICSListEndpoint.Map(icsGroup);
        GetICSByIdEndpoint.Map(icsGroup);
    }
}
