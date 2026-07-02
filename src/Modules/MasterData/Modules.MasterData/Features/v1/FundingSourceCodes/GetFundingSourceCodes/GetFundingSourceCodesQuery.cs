using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodes;

public sealed record GetFundingSourceCodesQuery(
    string? Keyword = null,
    string? FundClusterCode = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponseOfFundingSourceCodeDto>;

public sealed record PagedResponseOfFundingSourceCodeDto(
    ICollection<FundingSourceCodeDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

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
