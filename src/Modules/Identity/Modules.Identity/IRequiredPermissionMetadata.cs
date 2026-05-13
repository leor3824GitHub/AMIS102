namespace AMIS.Modules.Identity;

public interface IRequiredPermissionMetadata
{
    HashSet<string> RequiredPermissions { get; }
}

