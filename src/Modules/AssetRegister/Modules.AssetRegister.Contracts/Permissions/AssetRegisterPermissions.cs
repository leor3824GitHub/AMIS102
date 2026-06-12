namespace AMIS.Modules.AssetRegister.Contracts.Permissions;

public static class AssetRegisterPermissions
{
    public static class Assets
    {
        public const string View     = "Permissions.AssetRegister.Assets.View";
        public const string Register = "Permissions.AssetRegister.Assets.Register";
        public const string Update   = "Permissions.AssetRegister.Assets.Update";
    }

    public static class Accountability
    {
        public const string View     = "Permissions.AssetRegister.Accountability.View";
        public const string Issue    = "Permissions.AssetRegister.Accountability.Issue";
        public const string Return   = "Permissions.AssetRegister.Accountability.Return";
    }

    /// <summary>
    /// Employee self-service permissions. These scope a user to <em>their own</em> ICS/PAR
    /// (records where they are the <c>ReceivedBy</c> accountable person) and never grant the
    /// officer-wide <see cref="Accountability"/> view. Bundle these into the "Employee" role.
    /// </summary>
    public static class MyAccountability
    {
        public const string View           = "Permissions.AssetRegister.MyAccountability.View";
        public const string Acknowledge    = "Permissions.AssetRegister.MyAccountability.Acknowledge";
        public const string Return         = "Permissions.AssetRegister.MyAccountability.Return";
        public const string ReportIncident = "Permissions.AssetRegister.MyAccountability.ReportIncident";
        public const string ConfirmCount   = "Permissions.AssetRegister.MyAccountability.ConfirmCount";
    }

    public static class Issuance
    {
        public const string View   = "Permissions.AssetRegister.Issuance.View";
        public const string Create = "Permissions.AssetRegister.Issuance.Create";
        public const string Update = "Permissions.AssetRegister.Issuance.Update";
    }

    public static class Count
    {
        public const string View   = "Permissions.AssetRegister.Count.View";
        public const string Create = "Permissions.AssetRegister.Count.Create";
        public const string Freeze = "Permissions.AssetRegister.Count.Freeze";
        public const string Record = "Permissions.AssetRegister.Count.Record";
        public const string Submit = "Permissions.AssetRegister.Count.Submit";
        public const string Close  = "Permissions.AssetRegister.Count.Close";
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

    public static class ReturnedProperty
    {
        public const string View   = "Permissions.AssetRegister.ReturnedProperty.View";
        public const string Create = "Permissions.AssetRegister.ReturnedProperty.Create";
        public const string Accept = "Permissions.AssetRegister.ReturnedProperty.Accept";
    }
}
