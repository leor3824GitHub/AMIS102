namespace FSH.Modules.AssetRegister;

public static class AssetRegisterModuleConstants
{
    public const string SchemaName = "asset_register";
    public const string MigrationsTable = "__EFMigrationsHistory_AssetRegister";

    public static class Permissions
    {
        public static class Assets
        {
            public const string View     = "Permissions.AssetRegister.Assets.View";
            public const string Register = "Permissions.AssetRegister.Assets.Register";
            public const string Update   = "Permissions.AssetRegister.Assets.Update";
            public const string Retire   = "Permissions.AssetRegister.Assets.Retire";
        }

        public static class Accountability
        {
            public const string View     = "Permissions.AssetRegister.Accountability.View";
            public const string Issue    = "Permissions.AssetRegister.Accountability.Issue";
            public const string Transfer = "Permissions.AssetRegister.Accountability.Transfer";
            public const string Return   = "Permissions.AssetRegister.Accountability.Return";
        }

        public static class Issuance
        {
            public const string View = "Permissions.AssetRegister.Issuance.View";
            public const string Post = "Permissions.AssetRegister.Issuance.Post";
        }

        public static class Count
        {
            public const string View    = "Permissions.AssetRegister.Count.View";
            public const string Create  = "Permissions.AssetRegister.Count.Create";
            public const string Record  = "Permissions.AssetRegister.Count.Record";
            public const string Submit  = "Permissions.AssetRegister.Count.Submit";
            public const string Close   = "Permissions.AssetRegister.Count.Close";
        }

        public static class Incident
        {
            public const string View    = "Permissions.AssetRegister.Incident.View";
            public const string File    = "Permissions.AssetRegister.Incident.File";
            public const string Resolve = "Permissions.AssetRegister.Incident.Resolve";
        }

        public static class Unserviceable
        {
            public const string View    = "Permissions.AssetRegister.Unserviceable.View";
            public const string File    = "Permissions.AssetRegister.Unserviceable.File";
            public const string Dispose = "Permissions.AssetRegister.Unserviceable.Dispose";
        }

        public static class Catalog
        {
            public const string View   = "Permissions.AssetRegister.Catalog.View";
            public const string Create = "Permissions.AssetRegister.Catalog.Create";
            public const string Update = "Permissions.AssetRegister.Catalog.Update";
            public const string Delete = "Permissions.AssetRegister.Catalog.Delete";
        }

        public static class Receiving
        {
            public const string View   = "Permissions.AssetRegister.Receiving.View";
            public const string Create = "Permissions.AssetRegister.Receiving.Create";
            public const string Delete = "Permissions.AssetRegister.Receiving.Delete";
        }
    }
}
