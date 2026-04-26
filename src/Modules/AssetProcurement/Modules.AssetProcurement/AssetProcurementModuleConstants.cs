namespace FSH.Modules.AssetProcurement;

public static class AssetProcurementModuleConstants
{
    public const string SchemaName = "asset_procurement";

    public static class Permissions
    {
        public static class AssetPurchaseRequests
        {
            public const string View   = "Permissions.AssetProcurement.AssetPurchaseRequests.View";
            public const string Create = "Permissions.AssetProcurement.AssetPurchaseRequests.Create";
            public const string Update = "Permissions.AssetProcurement.AssetPurchaseRequests.Update";
            public const string Submit = "Permissions.AssetProcurement.AssetPurchaseRequests.Submit";
            public const string Approve = "Permissions.AssetProcurement.AssetPurchaseRequests.Approve";
            public const string Reject = "Permissions.AssetProcurement.AssetPurchaseRequests.Reject";
            public const string Cancel = "Permissions.AssetProcurement.AssetPurchaseRequests.Cancel";
        }

        public static class AssetPurchaseOrders
        {
            public const string View   = "Permissions.AssetProcurement.AssetPurchaseOrders.View";
            public const string Create = "Permissions.AssetProcurement.AssetPurchaseOrders.Create";
            public const string Update = "Permissions.AssetProcurement.AssetPurchaseOrders.Update";
            public const string Issue  = "Permissions.AssetProcurement.AssetPurchaseOrders.Issue";
            public const string Cancel = "Permissions.AssetProcurement.AssetPurchaseOrders.Cancel";
        }

        public static class AssetIARs
        {
            public const string View   = "Permissions.AssetProcurement.AssetIARs.View";
            public const string Create = "Permissions.AssetProcurement.AssetIARs.Create";
            public const string Update = "Permissions.AssetProcurement.AssetIARs.Update";
            public const string Accept = "Permissions.AssetProcurement.AssetIARs.Accept";
            public const string Reject = "Permissions.AssetProcurement.AssetIARs.Reject";
        }
    }
}
