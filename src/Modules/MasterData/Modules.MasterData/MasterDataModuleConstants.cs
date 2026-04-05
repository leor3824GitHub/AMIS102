namespace FSH.Modules.MasterData;

public static class MasterDataModuleConstants
{
    public const string SchemaName = "employee";

    public static class Permissions
    {
        public static class Lookup
        {
            public const string View = "Permissions.MasterData.Lookup.View";
        }

        public static class Employees
        {
            public const string View = "Permissions.MasterData.Employees.View";
            public const string Create = "Permissions.MasterData.Employees.Create";
            public const string Update = "Permissions.MasterData.Employees.Update";
            public const string Delete = "Permissions.MasterData.Employees.Delete";
        }

        public static class Offices
        {
            public const string View = "Permissions.MasterData.Offices.View";
            public const string Create = "Permissions.MasterData.Offices.Create";
            public const string Update = "Permissions.MasterData.Offices.Update";
            public const string Delete = "Permissions.MasterData.Offices.Delete";
        }

        public static class Departments
        {
            public const string View = "Permissions.MasterData.Departments.View";
            public const string Create = "Permissions.MasterData.Departments.Create";
            public const string Update = "Permissions.MasterData.Departments.Update";
            public const string Delete = "Permissions.MasterData.Departments.Delete";
        }

        public static class Positions
        {
            public const string View = "Permissions.MasterData.Positions.View";
            public const string Create = "Permissions.MasterData.Positions.Create";
            public const string Update = "Permissions.MasterData.Positions.Update";
            public const string Delete = "Permissions.MasterData.Positions.Delete";
        }

        public static class UnitOfMeasures
        {
            public const string View = "Permissions.MasterData.UnitOfMeasures.View";
            public const string Create = "Permissions.MasterData.UnitOfMeasures.Create";
            public const string Update = "Permissions.MasterData.UnitOfMeasures.Update";
            public const string Delete = "Permissions.MasterData.UnitOfMeasures.Delete";
        }

        public static class Suppliers
        {
            public const string View = "Permissions.MasterData.Suppliers.View";
            public const string Create = "Permissions.MasterData.Suppliers.Create";
            public const string Update = "Permissions.MasterData.Suppliers.Update";
            public const string Delete = "Permissions.MasterData.Suppliers.Delete";
        }

        public static class Categories
        {
            public const string View = "Permissions.MasterData.Categories.View";
            public const string Create = "Permissions.MasterData.Categories.Create";
            public const string Update = "Permissions.MasterData.Categories.Update";
            public const string Delete = "Permissions.MasterData.Categories.Delete";
        }

        public static class ReportSignatories
        {
            public const string View = "Permissions.MasterData.ReportSignatories.View";
            public const string Create = "Permissions.MasterData.ReportSignatories.Create";
            public const string Update = "Permissions.MasterData.ReportSignatories.Update";
            public const string Delete = "Permissions.MasterData.ReportSignatories.Delete";
        }

        public static class OrganizationProfile
        {
            public const string View = "Permissions.MasterData.OrganizationProfile.View";
            public const string Manage = "Permissions.MasterData.OrganizationProfile.Manage";
        }
    }
}