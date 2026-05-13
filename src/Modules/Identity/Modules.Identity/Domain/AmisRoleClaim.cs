using Microsoft.AspNetCore.Identity;

namespace AMIS.Modules.Identity.Domain;

public class AmisRoleClaim : IdentityRoleClaim<string>
{
    public string? CreatedBy { get; init; }
    public DateTimeOffset CreatedOn { get; init; }
}


