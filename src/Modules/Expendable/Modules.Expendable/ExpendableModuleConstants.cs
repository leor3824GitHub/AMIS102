namespace AMIS.Modules.Expendable;

public static class ExpendableModuleConstants
{
    public const string SchemaName = "expendable";
    public const string MigrationsTable = "__EFMigrationsHistory";

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
