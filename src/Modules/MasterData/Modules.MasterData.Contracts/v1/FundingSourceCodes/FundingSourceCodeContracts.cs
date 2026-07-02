namespace AMIS.Modules.MasterData.Contracts.v1.FundingSourceCodes;

public sealed record FundingSourceCodeDto(
    Guid Id,
    string Code,
    string FundClusterCode,
    string? FinancingSource,
    string? Authorization,
    string? FundCategory,
    string? FundSubCategory,
    string? Description,
    string? DepartmentName,
    string? AgencyName,
    bool IsActive);

public sealed record CreateFundingSourceCodeCommand(
    string Code,
    string FundClusterCode,
    string? FinancingSource = null,
    string? Authorization = null,
    string? FundCategory = null,
    string? FundSubCategory = null,
    string? Description = null,
    string? DepartmentName = null,
    string? AgencyName = null);

public sealed record UpdateFundingSourceCodeCommand(
    Guid Id,
    string Code,
    string FundClusterCode,
    string? FinancingSource = null,
    string? Authorization = null,
    string? FundCategory = null,
    string? FundSubCategory = null,
    string? Description = null,
    string? DepartmentName = null,
    string? AgencyName = null,
    bool IsActive = true);
