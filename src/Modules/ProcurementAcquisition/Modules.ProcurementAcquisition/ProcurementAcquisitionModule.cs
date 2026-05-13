using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CreatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.UpdatePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SubmitPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.ApprovePurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.RejectPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.CancelPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.GetPurchaseRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseRequests.SearchPurchaseRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.GetCanvassRequest;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.SearchCanvassRequests;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CreatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.UpdatePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.IssuePurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.CancelPurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.GetPurchaseOrder;
using AMIS.Modules.ProcurementAcquisition.Features.v1.PurchaseOrders.SearchPurchaseOrders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.ProcurementAcquisition;

public class ProcurementAcquisitionModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Purchase Requests", "View", "Procurement.PurchaseRequests", IsBasic: true),
        new("Create Purchase Requests", "Create", "Procurement.PurchaseRequests"),
        new("Update Purchase Requests", "Update", "Procurement.PurchaseRequests"),
        new("Submit Purchase Requests", "Submit", "Procurement.PurchaseRequests"),
        new("Approve Purchase Requests", "Approve", "Procurement.PurchaseRequests"),
        new("Reject Purchase Requests", "Reject", "Procurement.PurchaseRequests"),
        new("Cancel Purchase Requests", "Cancel", "Procurement.PurchaseRequests"),

        new("View Canvass Requests", "View", "Procurement.CanvassRequests", IsBasic: true),
        new("Create Canvass Requests", "Create", "Procurement.CanvassRequests"),
        new("Update Canvass Requests", "Update", "Procurement.CanvassRequests"),
        new("Award Canvass Requests", "Award", "Procurement.CanvassRequests"),
        new("Cancel Canvass Requests", "Cancel", "Procurement.CanvassRequests"),

        new("View Purchase Orders", "View", "Procurement.PurchaseOrders", IsBasic: true),
        new("Create Purchase Orders", "Create", "Procurement.PurchaseOrders"),
        new("Update Purchase Orders", "Update", "Procurement.PurchaseOrders"),
        new("Issue Purchase Orders", "Issue", "Procurement.PurchaseOrders"),
        new("Cancel Purchase Orders", "Cancel", "Procurement.PurchaseOrders"),
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        PermissionConstants.Register(RegisteredPermissions);

        services.AddHeroDbContext<ProcurementDbContext>();
        services.AddScoped<IDbInitializer, ProcurementDbInitializer>();
        services.AddHostedService<AMIS.Modules.ProcurementAcquisition.Provisioning.ProcurementDbInitializerHostedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/procurement")
            .WithTags("Procurement")
            .WithApiVersionSet(apiVersionSet);

        var purchaseRequestsGroup = moduleGroup.MapGroup("/purchase-requests");
        var canvassRequestsGroup = moduleGroup.MapGroup("/canvass-requests");
        var purchaseOrdersGroup = moduleGroup.MapGroup("/purchase-orders");

        // Purchase Requests
        CreatePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        UpdatePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        SubmitPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        ApprovePurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        RejectPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        CancelPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        GetPurchaseRequestEndpoint.Map(purchaseRequestsGroup);
        SearchPurchaseRequestsEndpoint.Map(purchaseRequestsGroup);

        // Canvass Requests
        CreateCanvassRequestEndpoint.Map(canvassRequestsGroup);
        AddQuotationEndpoint.Map(canvassRequestsGroup);
        UpdateQuotationEndpoint.Map(canvassRequestsGroup);
        AwardCanvassEndpoint.Map(canvassRequestsGroup);
        GetCanvassRequestEndpoint.Map(canvassRequestsGroup);
        SearchCanvassRequestsEndpoint.Map(canvassRequestsGroup);

        // Purchase Orders
        CreatePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        UpdatePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        IssuePurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        CancelPurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        GetPurchaseOrderEndpoint.Map(purchaseOrdersGroup);
        SearchPurchaseOrdersEndpoint.Map(purchaseOrdersGroup);
    }
}


