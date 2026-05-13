using Microsoft.AspNetCore.Identity;

namespace AMIS.Modules.Identity.Domain;

public class AmisRole : IdentityRole
{
    public string? Description { get; set; }

    public AmisRole(string name, string? description = null)
        : base(name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Description = description;
        NormalizedName = name.ToUpperInvariant();
    }
}


