using AMIS.Framework.Core.Domain;

namespace AMIS.Modules.MasterData.Domain;

/// <summary>
/// A UACS Funding Source Code — the full national funding-source taxonomy (financing source,
/// authorization, fund category/sub-category) that rolls up to a <see cref="FundCluster"/>.
/// Primarily consumed by Procurement Planning (PPMP/APP). The 8-digit <see cref="Code"/> is the
/// UACS funding source code; <see cref="FundClusterCode"/> links back to the owning cluster.
/// Seeded from an embedded CSV in <c>MasterDataDbInitializer</c>; editable via CRUD.
/// </summary>
public sealed class FundingSourceCode : AggregateRoot<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string FundClusterCode { get; private set; } = default!;
    public string? FinancingSource { get; private set; }
    public string? Authorization { get; private set; }
    public string? FundCategory { get; private set; }
    public string? FundSubCategory { get; private set; }
    public string? Description { get; private set; }
    public string? DepartmentName { get; private set; }
    public string? AgencyName { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; private set; } = [];

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }
    public bool IsDeleted { get; private set; }

    public static FundingSourceCode Create(
        string code,
        string fundClusterCode,
        string? financingSource = null,
        string? authorization = null,
        string? fundCategory = null,
        string? fundSubCategory = null,
        string? description = null,
        string? departmentName = null,
        string? agencyName = null)
    {
        return new FundingSourceCode
        {
            Id = Guid.NewGuid(),
            Code = code,
            FundClusterCode = fundClusterCode,
            FinancingSource = financingSource,
            Authorization = authorization,
            FundCategory = fundCategory,
            FundSubCategory = fundSubCategory,
            Description = description,
            DepartmentName = departmentName,
            AgencyName = agencyName,
            IsActive = true,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string code,
        string fundClusterCode,
        string? financingSource,
        string? authorization,
        string? fundCategory,
        string? fundSubCategory,
        string? description,
        string? departmentName,
        string? agencyName,
        bool isActive)
    {
        Code = code;
        FundClusterCode = fundClusterCode;
        FinancingSource = financingSource;
        Authorization = authorization;
        FundCategory = fundCategory;
        FundSubCategory = fundSubCategory;
        Description = description;
        DepartmentName = departmentName;
        AgencyName = agencyName;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
