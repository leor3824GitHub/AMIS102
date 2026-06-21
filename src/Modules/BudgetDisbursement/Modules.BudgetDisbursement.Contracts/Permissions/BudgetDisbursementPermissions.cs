namespace AMIS.Modules.BudgetDisbursement.Contracts.Permissions;

public static class BudgetDisbursementPermissions
{
    public static class DisbursementVouchers
    {
        public const string View = "Permissions.BudgetDisbursement.DisbursementVouchers.View";
        public const string Create = "Permissions.BudgetDisbursement.DisbursementVouchers.Create";
        public const string Delete = "Permissions.BudgetDisbursement.DisbursementVouchers.Delete";
        public const string Approve = "Permissions.BudgetDisbursement.DisbursementVouchers.Approve";
        public const string Pay = "Permissions.BudgetDisbursement.DisbursementVouchers.Pay";
        public const string Return = "Permissions.BudgetDisbursement.DisbursementVouchers.Return";
        public const string Cancel = "Permissions.BudgetDisbursement.DisbursementVouchers.Cancel";
    }

    public static class BudgetUtilizationRequests
    {
        public const string View = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.View";
        public const string Create = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.Create";
        public const string Delete = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.Delete";
        public const string Obligate = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.Obligate";
        public const string Utilize = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.Utilize";
        public const string Cancel = "Permissions.BudgetDisbursement.BudgetUtilizationRequests.Cancel";
    }

    public static class SignedDocuments
    {
        public const string View = "Permissions.BudgetDisbursement.SignedDocuments.View";
        public const string Upload = "Permissions.BudgetDisbursement.SignedDocuments.Upload";
    }

    public static class Settings
    {
        public const string View = "Permissions.BudgetDisbursement.Settings.View";
        public const string Update = "Permissions.BudgetDisbursement.Settings.Update";
    }
}
