namespace AMIS.Modules.Expendable.Contracts.Permissions;

public static class ExpendablePermissions
{
    public const string ViewAll = "Permissions.Expendable.View";
    public const string CreateAll = "Permissions.Expendable.Create";
    public const string UpdateAll = "Permissions.Expendable.Update";
    public const string DeleteAll = "Permissions.Expendable.Delete";

    public static class Products
    {
        public const string View = "Permissions.Expendable.Products.View";
        public const string Create = "Permissions.Expendable.Products.Create";
        public const string Update = "Permissions.Expendable.Products.Update";
        public const string Delete = "Permissions.Expendable.Products.Delete";
        public const string Activate = "Permissions.Expendable.Products.Activate";
        public const string Deactivate = "Permissions.Expendable.Products.Deactivate";
        public const string Discontinue = "Permissions.Expendable.Products.Discontinue";
        public const string MarkOutOfStock = "Permissions.Expendable.Products.MarkOutOfStock";
    }

    // Purchase orders + receiving moved to ProcurementAcquisition; Expendable no longer owns PO permissions.

    public static class SupplyRequests
    {
        public const string View = "Permissions.Expendable.SupplyRequests.View";
        public const string Create = "Permissions.Expendable.SupplyRequests.Create";
        public const string Update = "Permissions.Expendable.SupplyRequests.Update";
        public const string Delete = "Permissions.Expendable.SupplyRequests.Delete";
        public const string Approve = "Permissions.Expendable.SupplyRequests.Approve";
        public const string Reject = "Permissions.Expendable.SupplyRequests.Reject";
        public const string Fulfill = "Permissions.Expendable.SupplyRequests.Fulfill";
        public const string Cancel = "Permissions.Expendable.SupplyRequests.Cancel";
    }

    public static class ShoppingCarts
    {
        public const string View = "Permissions.Expendable.ShoppingCarts.View";
        public const string Create = "Permissions.Expendable.ShoppingCarts.Create";
        public const string Edit = "Permissions.Expendable.ShoppingCarts.Edit";
        public const string Clear = "Permissions.Expendable.ShoppingCarts.Clear";
        public const string Convert = "Permissions.Expendable.ShoppingCarts.Convert";
    }

    public static class Inventory
    {
        public const string View = "Permissions.Expendable.Inventory.View";
        public const string Receive = "Permissions.Expendable.Inventory.Receive";
        public const string Consume = "Permissions.Expendable.Inventory.Consume";
        public const string ViewReports = "Permissions.Expendable.Inventory.ViewReports";
    }
}
