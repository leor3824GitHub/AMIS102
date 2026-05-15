using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.AssetProcurement.Data;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CreateAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.UpdateAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.SubmitAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.ApproveAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.RejectAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.CancelAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.GetAssetPurchaseRequest;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseRequests.SearchAssetPurchaseRequests;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CreateAssetPurchaseOrder;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.UpdateAssetPurchaseOrder;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.IssueAssetPurchaseOrder;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.CancelAssetPurchaseOrder;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.GetAssetPurchaseOrder;
using AMIS.Modules.AssetProcurement.Features.v1.AssetPurchaseOrders.SearchAssetPurchaseOrders;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CreateAssetIAR;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.UpdateAssetIAR;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.AcceptAssetIAR;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.GetAssetIAR;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.SearchAssetIARs;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.SubmitForInspection;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.ReassignInspector;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.RecordInspection;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.AssignPropertyNo;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.ExpandLineByQuantity;
using AMIS.Modules.AssetProcurement.Features.v1.AssetIARs.CancelAssetIAR;
using AMIS.Modules.AssetProcurement.Provisioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.AssetProcurement;

public class AssetProcurementModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Asset Purchase Requests",   "View",    "AssetProcurement.AssetPurchaseRequests", IsBasic: true),
        new("Create Asset Purchase Requests", "Create",  "AssetProcurement.AssetPurchaseRequests"),
        new("Update Asset Purchase Requests", "Update",  "AssetProcurement.AssetPurchaseRequests"),
        new("Submit Asset Purchase Requests", "Submit",  "AssetProcurement.AssetPurchaseRequests"),
        new("Approve Asset Purchase Requests","Approve", "AssetProcurement.AssetPurchaseRequests"),
        new("Reject Asset Purchase Requests", "Reject",  "AssetProcurement.AssetPurchaseRequests"),
        new("Cancel Asset Purchase Requests", "Cancel",  "AssetProcurement.AssetPurchaseRequests"),

        new("View Asset Purchase Orders",     "View",    "AssetProcurement.AssetPurchaseOrders", IsBasic: true),
        new("Create Asset Purchase Orders",   "Create",  "AssetProcurement.AssetPurchaseOrders"),
        new("Update Asset Purchase Orders",   "Update",  "AssetProcurement.AssetPurchaseOrders"),
        new("Issue Asset Purchase Orders",    "Issue",   "AssetProcurement.AssetPurchaseOrders"),
        new("Cancel Asset Purchase Orders",   "Cancel",  "AssetProcurement.AssetPurchaseOrders"),

        new("View Asset IARs",                "View",                "AssetProcurement.AssetIARs", IsBasic: true),
        new("Create Asset IARs",              "Create",              "AssetProcurement.AssetIARs"),
        new("Update Asset IARs",              "Update",              "AssetProcurement.AssetIARs"),
        new("Accept Asset IARs",              "Accept",              "AssetProcurement.AssetIARs"),
        new("Submit Asset IARs for Inspection", "SubmitForInspection", "AssetProcurement.AssetIARs"),
        new("Inspect Asset IARs",             "Inspect",             "AssetProcurement.AssetIARs"),
        new("Assign Property No on Asset IARs", "AssignPropertyNo",  "AssetProcurement.AssetIARs"),
        new("Cancel Asset IARs",              "Cancel",              "AssetProcurement.AssetIARs"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<AssetProcurementDbContext>();
        services.AddScoped<IDbInitializer, AssetProcurementDbInitializer>();
        services.AddHostedService<AssetProcurementDbInitializerHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/asset-procurement")
            .WithTags("Asset Procurement")
            .WithApiVersionSet(apiVersionSet);

        var prGroup  = moduleGroup.MapGroup("/purchase-requests");
        var poGroup  = moduleGroup.MapGroup("/purchase-orders");
        var iarGroup = moduleGroup.MapGroup("/iars");

        // Purchase Requests
        CreateAssetPurchaseRequestEndpoint.Map(prGroup);
        UpdateAssetPurchaseRequestEndpoint.Map(prGroup);
        SubmitAssetPurchaseRequestEndpoint.Map(prGroup);
        ApproveAssetPurchaseRequestEndpoint.Map(prGroup);
        RejectAssetPurchaseRequestEndpoint.Map(prGroup);
        CancelAssetPurchaseRequestEndpoint.Map(prGroup);
        GetAssetPurchaseRequestEndpoint.Map(prGroup);
        SearchAssetPurchaseRequestsEndpoint.Map(prGroup);

        // Purchase Orders
        CreateAssetPurchaseOrderEndpoint.Map(poGroup);
        UpdateAssetPurchaseOrderEndpoint.Map(poGroup);
        IssueAssetPurchaseOrderEndpoint.Map(poGroup);
        CancelAssetPurchaseOrderEndpoint.Map(poGroup);
        GetAssetPurchaseOrderEndpoint.Map(poGroup);
        SearchAssetPurchaseOrdersEndpoint.Map(poGroup);

        // IARs
        CreateAssetIAREndpoint.Map(iarGroup);
        UpdateAssetIAREndpoint.Map(iarGroup);
        SubmitIARForInspectionEndpoint.Map(iarGroup);
        ReassignInspectorEndpoint.Map(iarGroup);
        RecordIARInspectionEndpoint.Map(iarGroup);
        AssignPropertyNoEndpoint.Map(iarGroup);
        ExpandLineByQuantityEndpoint.Map(iarGroup);
        AcceptAssetIAREndpoint.Map(iarGroup);
        CancelAssetIAREndpoint.Map(iarGroup);
        GetAssetIAREndpoint.Map(iarGroup);
        SearchAssetIARsEndpoint.Map(iarGroup);
    }
}


