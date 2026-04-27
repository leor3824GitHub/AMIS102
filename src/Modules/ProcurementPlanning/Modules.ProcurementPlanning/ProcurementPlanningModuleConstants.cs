namespace FSH.Modules.ProcurementPlanning;

public static class ProcurementPlanningModuleConstants
{
    public const string SchemaName = "procurement_planning";

    public static class Permissions
    {
        public static class Ppmps
        {
            public const string View    = "Permissions.ProcurementPlanning.Ppmps.View";
            public const string Create  = "Permissions.ProcurementPlanning.Ppmps.Create";
            public const string Update  = "Permissions.ProcurementPlanning.Ppmps.Update";
            public const string Submit  = "Permissions.ProcurementPlanning.Ppmps.Submit";
            public const string Approve = "Permissions.ProcurementPlanning.Ppmps.Approve";
            public const string Return  = "Permissions.ProcurementPlanning.Ppmps.Return";
            public const string Amend   = "Permissions.ProcurementPlanning.Ppmps.Amend";
        }

        public static class AnnualProcurementPlans
        {
            public const string View        = "Permissions.ProcurementPlanning.Apps.View";
            public const string Create      = "Permissions.ProcurementPlanning.Apps.Create";
            public const string Consolidate = "Permissions.ProcurementPlanning.Apps.Consolidate";
            public const string Publish     = "Permissions.ProcurementPlanning.Apps.Publish";
            public const string Amend       = "Permissions.ProcurementPlanning.Apps.Amend";
        }
    }
}
