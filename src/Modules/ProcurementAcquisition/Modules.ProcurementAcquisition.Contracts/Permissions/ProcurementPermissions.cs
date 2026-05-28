namespace AMIS.Modules.ProcurementAcquisition.Contracts.Permissions;

public static class ProcurementPermissions
{
    public static class PurchaseRequests
    {
        public const string View = "Permissions.Procurement.PurchaseRequests.View";
        public const string Create = "Permissions.Procurement.PurchaseRequests.Create";
        public const string Update = "Permissions.Procurement.PurchaseRequests.Update";
        public const string Submit = "Permissions.Procurement.PurchaseRequests.Submit";
        public const string CertifyFundsAvailable = "Permissions.Procurement.PurchaseRequests.CertifyFundsAvailable";
        public const string Approve = "Permissions.Procurement.PurchaseRequests.Approve";
        public const string ReturnForRevision = "Permissions.Procurement.PurchaseRequests.ReturnForRevision";
        public const string Reject = "Permissions.Procurement.PurchaseRequests.Reject";
        public const string Cancel = "Permissions.Procurement.PurchaseRequests.Cancel";
    }

    public static class CanvassRequests
    {
        public const string View = "Permissions.Procurement.CanvassRequests.View";
        public const string Create = "Permissions.Procurement.CanvassRequests.Create";
        public const string Update = "Permissions.Procurement.CanvassRequests.Update";
        public const string Award = "Permissions.Procurement.CanvassRequests.Award";
        public const string Cancel = "Permissions.Procurement.CanvassRequests.Cancel";
    }

    public static class PurchaseOrders
    {
        public const string View = "Permissions.Procurement.PurchaseOrders.View";
        public const string Create = "Permissions.Procurement.PurchaseOrders.Create";
        public const string Update = "Permissions.Procurement.PurchaseOrders.Update";
        public const string Submit = "Permissions.Procurement.PurchaseOrders.Submit";
        public const string CertifyFundsAvailable = "Permissions.Procurement.PurchaseOrders.CertifyFundsAvailable";
        public const string Issue = "Permissions.Procurement.PurchaseOrders.Issue";
        public const string Cancel = "Permissions.Procurement.PurchaseOrders.Cancel";
    }

    public static class AssetIARs
    {
        public const string View = "Permissions.Procurement.AssetIARs.View";
        public const string Create = "Permissions.Procurement.AssetIARs.Create";
        public const string Update = "Permissions.Procurement.AssetIARs.Update";
        public const string Accept = "Permissions.Procurement.AssetIARs.Accept";
        public const string SubmitForInspection = "Permissions.Procurement.AssetIARs.SubmitForInspection";
        public const string Inspect = "Permissions.Procurement.AssetIARs.Inspect";
        public const string AssignPropertyNo = "Permissions.Procurement.AssetIARs.AssignPropertyNo";
        public const string ExpandLine = "Permissions.Procurement.AssetIARs.ExpandLine";
        public const string Cancel = "Permissions.Procurement.AssetIARs.Cancel";
    }
}
