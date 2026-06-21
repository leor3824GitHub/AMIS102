namespace AMIS.Modules.BudgetDisbursement.Domain.Settings;

public sealed class BudgetDisbursementModuleSettings
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = default!;
    public bool WatermarkSignedCopies { get; set; } = true;

    // DV signatory overrides — null means "fall back to org profile"
    public string? DvSectionAName { get; set; }
    public string? DvSectionADesignation { get; set; }
    public string? DvSectionBName { get; set; }
    public string? DvSectionBDesignation { get; set; }
    public string? DvSectionCName { get; set; }
    public string? DvSectionCDesignation { get; set; }

    // BUR signatory overrides — null means "fall back to org profile"
    public string? BurSectionAName { get; set; }
    public string? BurSectionADesignation { get; set; }
    public string? BurSectionBName { get; set; }
    public string? BurSectionBDesignation { get; set; }

    public static BudgetDisbursementModuleSettings CreateDefault(string tenantId) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        WatermarkSignedCopies = true
    };
}
