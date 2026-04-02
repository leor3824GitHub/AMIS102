using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Persistence;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Modules;
using FSH.Modules.Expendable.Contracts.v1.Cart;
using FSH.Modules.Expendable.Contracts.v1.Products;
using FSH.Modules.Expendable.Contracts.v1.Purchases;
using FSH.Modules.Expendable.Contracts.v1.Requests;
using FSH.Modules.Expendable.Contracts.v1.Warehouse;
using FSH.Modules.Expendable.Data;
using FSH.Modules.Expendable.Features.v1.Products.CreateProduct;
using FSH.Modules.Expendable.Features.v1.Products.UpdateProduct;
using FSH.Modules.Expendable.Features.v1.Products.ActivateProduct;
using FSH.Modules.Expendable.Features.v1.Products.DeactivateProduct;
using FSH.Modules.Expendable.Features.v1.Products.DiscontinueProduct;
using FSH.Modules.Expendable.Features.v1.Products.MarkOutOfStock;
using FSH.Modules.Expendable.Features.v1.Products.DeleteProduct;
using FSH.Modules.Expendable.Features.v1.Products.GetProduct;
using FSH.Modules.Expendable.Features.v1.Products.GetProductCatalogCards;
using FSH.Modules.Expendable.Features.v1.Products.ListActiveProducts;
using FSH.Modules.Expendable.Features.v1.Products.SearchProducts;
using FSH.Modules.Expendable.Features.v1.Purchases.CreatePurchaseOrder;
using FSH.Modules.Expendable.Features.v1.Purchases.AddPurchaseLineItem;
using FSH.Modules.Expendable.Features.v1.Purchases.RemovePurchaseLineItem;
using FSH.Modules.Expendable.Features.v1.Purchases.SubmitPurchaseOrder;
using FSH.Modules.Expendable.Features.v1.Purchases.ApprovePurchaseOrder;
using FSH.Modules.Expendable.Features.v1.Purchases.RecordPurchaseReceipt;
using FSH.Modules.Expendable.Features.v1.Purchases.CancelPurchaseOrder;
using FSH.Modules.Expendable.Features.v1.Purchases.GetPurchase;
using FSH.Modules.Expendable.Features.v1.Purchases.GetPurchasesBySupplier;
using FSH.Modules.Expendable.Features.v1.Purchases.SearchPurchases;
using FSH.Modules.Expendable.Features.v1.Requests.CreateSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.SubmitSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.ApproveSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.GetSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.SearchSupplyRequests;
using FSH.Modules.Expendable.Features.v1.Requests.RejectSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.GetEmployeeSupplyRequests;
using FSH.Modules.Expendable.Features.v1.Requests.FulfillSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Requests.CancelSupplyRequest;
using FSH.Modules.Expendable.Features.v1.Reports.GetDepartmentIssuanceReport;
using FSH.Modules.Expendable.Features.v1.Reports.GetEmployeeIssuanceHistory;
using FSH.Modules.Expendable.Features.v1.Cart.GetOrCreateCart;
using FSH.Modules.Expendable.Features.v1.Cart.AddToCart;
using FSH.Modules.Expendable.Features.v1.Cart.GetCart;
using FSH.Modules.Expendable.Features.v1.Cart.ConvertCartToRequest;
using FSH.Modules.Expendable.Features.v1.Cart.RemoveFromCart;
using FSH.Modules.Expendable.Features.v1.Cart.ClearCart;
using FSH.Modules.Expendable.Features.v1.Warehouse.RecordInspection;
using FSH.Modules.Expendable.Features.v1.Warehouse.ReserveProductInventory;
using FSH.Modules.Expendable.Features.v1.Warehouse.CancelProductInventoryReservation;
using FSH.Modules.Expendable.Features.v1.Warehouse.IssueFromProductInventory;
using FSH.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryReturned;
using FSH.Modules.Expendable.Features.v1.Warehouse.MarkRejectedInventoryDisposed;
using FSH.Modules.Expendable.Features.v1.Warehouse.GetProductInventory;
using FSH.Modules.Expendable.Features.v1.Warehouse.SearchProductInventory;
using FSH.Modules.Expendable.Features.v1.Warehouse.GetWarehouseStockLevels;
using FSH.Modules.Expendable.Features.v1.Warehouse.GetRejectedInventory;
using FSH.Modules.Expendable.Features.v1.Warehouse.GetPendingInspections;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FSH.Modules.Expendable;

public class ExpendableModule : IModule
{
    private static readonly IReadOnlyList<FshPermission> RegisteredPermissions =
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

        new("View Expendable Purchases", "View", "Expendable.Purchases", IsBasic: true),
        new("Create Expendable Purchases", "Create", "Expendable.Purchases"),
        new("Update Expendable Purchases", "Update", "Expendable.Purchases"),
        new("Delete Expendable Purchases", "Delete", "Expendable.Purchases"),
        new("Approve Expendable Purchases", "Approve", "Expendable.Purchases"),
        new("Receive Expendable Purchases", "Receive", "Expendable.Purchases"),

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
        services.AddHostedService<FSH.Modules.Expendable.Provisioning.ExpendableDbInitializerHostedService>();

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
        var purchasesGroup = moduleGroup.MapGroup("/purchases");
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

        // Purchase Order Endpoints - Vertical Slices
        CreatePurchaseOrderEndpoint.Map(purchasesGroup);
        AddPurchaseLineItemEndpoint.Map(purchasesGroup);
        RemovePurchaseLineItemEndpoint.Map(purchasesGroup);
        SubmitPurchaseOrderEndpoint.Map(purchasesGroup);
        ApprovePurchaseOrderEndpoint.Map(purchasesGroup);
        RecordPurchaseReceiptEndpoint.Map(purchasesGroup);
        CancelPurchaseOrderEndpoint.Map(purchasesGroup);
        GetPurchaseEndpoint.Map(purchasesGroup);
        SearchPurchasesEndpoint.Map(purchasesGroup);
        GetPurchasesBySupplierEndpoint.Map(purchasesGroup);

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

        // Warehouse Endpoints - Vertical Slices
        RecordInspectionEndpoint.Map(warehouseGroup);
        ReserveProductInventoryEndpoint.Map(warehouseGroup);
        CancelProductInventoryReservationEndpoint.Map(warehouseGroup);
        IssueFromProductInventoryEndpoint.Map(warehouseGroup);
        MarkRejectedInventoryReturnedEndpoint.Map(warehouseGroup);
        MarkRejectedInventoryDisposedEndpoint.Map(warehouseGroup);
        GetProductInventoryEndpoint.Map(warehouseGroup);
        SearchProductInventoryEndpoint.Map(warehouseGroup);
        GetWarehouseStockLevelsEndpoint.Map(warehouseGroup);
        GetRejectedInventoryEndpoint.Map(warehouseGroup);
        GetPendingInspectionsEndpoint.Map(warehouseGroup);

        // Issuance Reports
        GetDepartmentIssuanceReportEndpoint.Map(reportsGroup);
        GetEmployeeIssuanceHistoryEndpoint.Map(reportsGroup);
    }
}
