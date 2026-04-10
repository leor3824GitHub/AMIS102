namespace FSH.Modules.AssetManagement;

public static class AssetManagementModuleConstants
{
    public const string SchemaName = "am";

    public static class Permissions
    {
        public static class SemiExpendableItems
        {
            public const string View   = "Permissions.AssetManagement.SemiExpendableItems.View";
            public const string Create = "Permissions.AssetManagement.SemiExpendableItems.Create";
            public const string Update = "Permissions.AssetManagement.SemiExpendableItems.Update";
            public const string Delete = "Permissions.AssetManagement.SemiExpendableItems.Delete";
        }

        public static class SemiExpendableProperties
        {
            public const string View   = "Permissions.AssetManagement.SemiExpendableProperties.View";
            public const string Create = "Permissions.AssetManagement.SemiExpendableProperties.Create";
            public const string Update = "Permissions.AssetManagement.SemiExpendableProperties.Update";
            public const string Delete = "Permissions.AssetManagement.SemiExpendableProperties.Delete";
        }

        public static class ReceivingReports
        {
            public const string View   = "Permissions.AssetManagement.ReceivingReports.View";
            public const string Create = "Permissions.AssetManagement.ReceivingReports.Create";
            public const string Update = "Permissions.AssetManagement.ReceivingReports.Update";
            public const string Delete = "Permissions.AssetManagement.ReceivingReports.Delete";
        }

        public static class InventoryCustodianSlips
        {
            public const string View   = "Permissions.AssetManagement.InventoryCustodianSlips.View";
            public const string Create = "Permissions.AssetManagement.InventoryCustodianSlips.Create";
            public const string Update = "Permissions.AssetManagement.InventoryCustodianSlips.Update";
            public const string Delete = "Permissions.AssetManagement.InventoryCustodianSlips.Delete";
        }
    }
}
