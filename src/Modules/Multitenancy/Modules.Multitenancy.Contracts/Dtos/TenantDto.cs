namespace AMIS.Modules.Multitenancy.Contracts.Dtos;

public sealed class TenantDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? ConnectionString { get; set; }
    public string AdminEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
    public string? Issuer { get; set; }

    /// <summary>The MasterData office this agency represents. Null when the tenant has not been linked yet.</summary>
    public Guid? OfficeId { get; set; }

    /// <summary>Display snapshot of the linked office's code.</summary>
    public string? OfficeCode { get; set; }
}
