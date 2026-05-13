namespace AMIS.Modules.Finance;

public static class FinanceModuleConstants
{
    public const string SchemaName = "finance";

    public static class Permissions
    {
        public static class DisbursementVouchers
        {
            public const string View = "Permissions.Finance.DisbursementVouchers.View";
            public const string Create = "Permissions.Finance.DisbursementVouchers.Create";
            public const string Approve = "Permissions.Finance.DisbursementVouchers.Approve";
            public const string Pay = "Permissions.Finance.DisbursementVouchers.Pay";
            public const string Return = "Permissions.Finance.DisbursementVouchers.Return";
            public const string Cancel = "Permissions.Finance.DisbursementVouchers.Cancel";
        }

        public static class BudgetUtilizationRecords
        {
            public const string View = "Permissions.Finance.BudgetUtilizationRecords.View";
            public const string Create = "Permissions.Finance.BudgetUtilizationRecords.Create";
            public const string Obligate = "Permissions.Finance.BudgetUtilizationRecords.Obligate";
            public const string Utilize = "Permissions.Finance.BudgetUtilizationRecords.Utilize";
            public const string Cancel = "Permissions.Finance.BudgetUtilizationRecords.Cancel";
        }
    }
}

