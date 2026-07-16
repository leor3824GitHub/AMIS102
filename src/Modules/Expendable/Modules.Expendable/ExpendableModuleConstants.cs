namespace AMIS.Modules.Expendable;

public static class ExpendableModuleConstants
{
    public const string SchemaName = "expendable";
    public const string MigrationsTable = "__EFMigrationsHistory";

    /// <summary>
    /// Single, well-known supply location used for all procurement-sourced receipts and as the default
    /// for warehouse/supply-request operations. The system is currently single-storeroom: there is no
    /// managed Warehouse table, so this fixed Id/Name stands in for "the central supply room". Making
    /// locations selectable (a managed lookup) is a future enhancement.
    /// </summary>
    public static class DefaultSupplyLocation
    {
        public static readonly Guid Id = new("11111111-1111-1111-1111-111111111111");
        public const string Name = "Central Supply Room";
    }

    /// <summary>
    /// Resolves a warehouse-location id to its display name. The system is single-storeroom, so this maps the
    /// one well-known id to <see cref="DefaultSupplyLocation.Name"/> (empty for any unknown id). This is the
    /// single seam to extend when locations become a managed lookup — replacing the removed
    /// <c>ProductInventory.WarehouseLocationName</c> snapshot column.
    /// </summary>
    public static string ResolveWarehouseName(Guid warehouseLocationId) =>
        warehouseLocationId == DefaultSupplyLocation.Id ? DefaultSupplyLocation.Name : string.Empty;

    /// <summary>Feature flag constants for Expendable module</summary>
    public static class Features
    {
        public const string ModuleName = "Expendable";
        public const string ProductManagement = $"{ModuleName}:ProductManagement";
        public const string PurchaseOrders = $"{ModuleName}:PurchaseOrders";
        public const string SupplyRequests = $"{ModuleName}:SupplyRequests";
        public const string ShoppingCart = $"{ModuleName}:ShoppingCart";
        public const string InventoryTracking = $"{ModuleName}:InventoryTracking";
    }
}
