using Asp.Versioning;
using AMIS.Framework.Persistence;
using AMIS.Framework.Shared.Constants;
using AMIS.Framework.Shared.Persistence;
using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Framework.Web.Modules;
using AMIS.Modules.Expendable.Contracts.v1.Cart;
using AMIS.Modules.Expendable.Contracts.v1.Products;
using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Contracts.v1.Warehouse;
using AMIS.Modules.Expendable.Data;
using AMIS.Modules.Expendable.Features.v1.Products.CreateProduct;
using AMIS.Modules.Expendable.Features.v1.Products.UpdateProduct;
using AMIS.Modules.Expendable.Features.v1.Products.ActivateProduct;
using AMIS.Modules.Expendable.Features.v1.Products.DeactivateProduct;
using AMIS.Modules.Expendable.Features.v1.Products.DiscontinueProduct;
using AMIS.Modules.Expendable.Features.v1.Products.MarkOutOfStock;
using AMIS.Modules.Expendable.Features.v1.Products.DeleteProduct;
using AMIS.Modules.Expendable.Features.v1.Products.GetProduct;
using AMIS.Modules.Expendable.Features.v1.Products.GetProductCatalogCards;
using AMIS.Modules.Expendable.Features.v1.Products.ListActiveProducts;
using AMIS.Modules.Expendable.Features.v1.Products.SearchProducts;
using AMIS.Modules.Expendable.Features.v1.Products.GetProductArticles;
using AMIS.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.SubmitSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.ApproveSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.GetSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.SearchSupplyRequests;
using AMIS.Modules.Expendable.Features.v1.Requests.RejectSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.GetEmployeeSupplyRequests;
using AMIS.Modules.Expendable.Features.v1.Requests.FulfillSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Requests.CancelSupplyRequest;
using AMIS.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;
using AMIS.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;
using AMIS.Modules.Expendable.Features.v1.Reports.GetPhysicalCountReport;
using AMIS.Modules.Expendable.Features.v1.Reports.GetStockCard;
using AMIS.Modules.Expendable.Features.v1.Cart.GetOrCreateCart;
using AMIS.Modules.Expendable.Features.v1.Cart.AddToCart;
using AMIS.Modules.Expendable.Features.v1.Cart.GetCart;
using AMIS.Modules.Expendable.Features.v1.Cart.ConvertCartToRequest;
using AMIS.Modules.Expendable.Features.v1.Cart.RemoveFromCart;
using AMIS.Modules.Expendable.Features.v1.Cart.ClearCart;
using AMIS.Modules.Expendable.Features.v1.Warehouse.ReserveProductInventory;
using AMIS.Modules.Expendable.Features.v1.Warehouse.CancelProductInventoryReservation;
using AMIS.Modules.Expendable.Features.v1.Warehouse.IssueFromProductInventory;
using AMIS.Modules.Expendable.Features.v1.Warehouse.GetProductInventory;
using AMIS.Modules.Expendable.Features.v1.Warehouse.SearchProductInventory;
using AMIS.Modules.Expendable.Features.v1.Warehouse.GetWarehouseStockLevels;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AMIS.Modules.Expendable;

public class ExpendableModule : IModule
{
    private static readonly IReadOnlyList<AmisPermission> RegisteredPermissions =
    [
        new("View Expendable", "View", "Expendable"),
        new("Create Expendable", "Create", "Expendable"),
        new("Update Expendable", "Update", "Expendable"),
        new("Delete Expendable", "Delete", "Expendable"),

        new("View Expendable Products", "View", "Expendable.Products", IsBasic: true),
        new("Create Expendable Products", "Create", "Expendable.Products"),
        new("Update Expendable Products", "Update", "Expendable.Products"),
        new("Delete Expendable Products", "Delete", "Expendable.Products"),
        new("Activate Expendable Products", "Activate", "Expendable.Products"),
        new("Deactivate Expendable Products", "Deactivate", "Expendable.Products"),
        new("Discontinue Expendable Products", "Discontinue", "Expendable.Products"),
        new("Mark Expendable Products Out Of Stock", "MarkOutOfStock", "Expendable.Products"),

        new("View Expendable Supply Requests", "View", "Expendable.SupplyRequests", IsBasic: true),
        new("Create Expendable Supply Requests", "Create", "Expendable.SupplyRequests"),
        new("Update Expendable Supply Requests", "Update", "Expendable.SupplyRequests"),
        new("Delete Expendable Supply Requests", "Delete", "Expendable.SupplyRequests"),
        new("Approve Expendable Supply Requests", "Approve", "Expendable.SupplyRequests"),
        new("Reject Expendable Supply Requests", "Reject", "Expendable.SupplyRequests"),
        new("Fulfill Expendable Supply Requests", "Fulfill", "Expendable.SupplyRequests"),
        new("Cancel Expendable Supply Requests", "Cancel", "Expendable.SupplyRequests", IsBasic: true),

        new("View Expendable Shopping Carts", "View", "Expendable.ShoppingCarts", IsBasic: true),
        new("Create Expendable Shopping Carts", "Create", "Expendable.ShoppingCarts"),
        new("Edit Expendable Shopping Carts", "Edit", "Expendable.ShoppingCarts"),
        new("Clear Expendable Shopping Carts", "Clear", "Expendable.ShoppingCarts"),
        new("Convert Expendable Shopping Carts", "Convert", "Expendable.ShoppingCarts"),

        new("View Expendable Inventory", "View", "Expendable.Inventory", IsBasic: true),
        new("Receive Expendable Inventory", "Receive", "Expendable.Inventory"),
        new("Consume Expendable Inventory", "Consume", "Expendable.Inventory"),
        new("View Expendable Inventory Reports", "ViewReports", "Expendable.Inventory")
    ];

    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var services = builder.Services;

        // Register module permissions so Identity role seeding can assign them.
        PermissionConstants.Register(RegisteredPermissions);

        // Register DbContext
        services.AddHeroDbContext<ExpendableDbContext>();

        // Register database initializer for multi-tenant migrations and seeding
        services.AddScoped<IDbInitializer, ExpendableDbInitializer>();

        // Register hosted service to initialize core database schema on app startup
        services.AddHostedService<AMIS.Modules.Expendable.Provisioning.ExpendableDbInitializerHostedService>();

        // Inbound integration: land accepted Supply IAR lines into ProductInventory.
        services.AddScoped<
            AMIS.Framework.Eventing.Abstractions.IIntegrationEventHandler<
                AMIS.Modules.ProcurementAcquisition.Contracts.v1.AssetInspectionAcceptanceReports.SupplyIARAcceptedEvent>,
            AMIS.Modules.Expendable.Integration.SupplyIARAcceptedEventConsumer>();

        // Fluent Validation will be auto-discovered
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var moduleGroup = endpoints
            .MapGroup("api/v{version:apiVersion}/expendable")
            .WithTags("Expendable")
            .WithApiVersionSet(apiVersionSet);

        var productsGroup = moduleGroup.MapGroup("/products");
        var supplyRequestsGroup = moduleGroup.MapGroup("/supply-requests");
        var cartGroup = moduleGroup.MapGroup("/cart");
        var warehouseGroup = moduleGroup.MapGroup("/warehouse");
        var reportsGroup = moduleGroup.MapGroup("/reports");

        // Product Endpoints - Vertical Slices
        CreateProductEndpoint.Map(productsGroup);
        UpdateProductEndpoint.Map(productsGroup);
        ActivateProductEndpoint.Map(productsGroup);
        DeactivateProductEndpoint.Map(productsGroup);
        DiscontinueProductEndpoint.Map(productsGroup);
        MarkOutOfStockEndpoint.Map(productsGroup);
        DeleteProductEndpoint.Map(productsGroup);
        GetProductEndpoint.Map(productsGroup);
        GetProductCatalogCardsEndpoint.Map(productsGroup);
        ListActiveProductsEndpoint.Map(productsGroup);
        SearchProductsEndpoint.Map(productsGroup);
        GetProductArticlesEndpoint.Map(productsGroup);

        // Purchase orders + receiving/inspection now live in ProcurementAcquisition. Expendable consumes
        // accepted Supply IAR lines into ProductInventory via SupplyIARAcceptedEvent (no PO endpoints here).

        // Supply Request Endpoints - Vertical Slices
        CreateSupplyRequestEndpoint.Map(supplyRequestsGroup);
        AddSupplyRequestItemEndpoint.Map(supplyRequestsGroup);
        RemoveSupplyRequestItemEndpoint.Map(supplyRequestsGroup);
        SubmitSupplyRequestEndpoint.Map(supplyRequestsGroup);
        ApproveSupplyRequestEndpoint.Map(supplyRequestsGroup);
        RejectSupplyRequestEndpoint.Map(supplyRequestsGroup);
        FulfillSupplyRequestEndpoint.Map(supplyRequestsGroup);
        CancelSupplyRequestEndpoint.Map(supplyRequestsGroup);
        GetSupplyRequestEndpoint.Map(supplyRequestsGroup);
        GetEmployeeSupplyRequestsEndpoint.Map(supplyRequestsGroup);
        SearchSupplyRequestsEndpoint.Map(supplyRequestsGroup);

        // Shopping Cart Endpoints - Vertical Slices
        GetOrCreateCartEndpoint.Map(cartGroup);
        AddToCartEndpoint.Map(cartGroup);
        GetCartEndpoint.Map(cartGroup);
        RemoveFromCartEndpoint.Map(cartGroup);
        ClearCartEndpoint.Map(cartGroup);
        ConvertCartToSupplyRequestEndpoint.Map(cartGroup);

        // Warehouse Endpoints - Vertical Slices (issue-side only; receiving/inspection moved to ProcurementAcquisition)
        ReserveProductInventoryEndpoint.Map(warehouseGroup);
        CancelProductInventoryReservationEndpoint.Map(warehouseGroup);
        IssueFromProductInventoryEndpoint.Map(warehouseGroup);
        GetProductInventoryEndpoint.Map(warehouseGroup);
        SearchProductInventoryEndpoint.Map(warehouseGroup);
        GetWarehouseStockLevelsEndpoint.Map(warehouseGroup);

        // Issuance Reports
        GetDepartmentIssuanceReportEndpoint.Map(reportsGroup);
        GetEmployeeIssuanceHistoryEndpoint.Map(reportsGroup);
        GetPhysicalCountReportEndpoint.Map(reportsGroup);
        GetStockCardEndpoint.Map(reportsGroup);
    }
}


