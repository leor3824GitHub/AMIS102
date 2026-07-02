using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.GetFundingSourceCodeById;

public sealed record GetFundingSourceCodeByIdQuery(Guid Id) : IQuery<FundingSourceCodeDetailsDto>;

public sealed record FundingSourceCodeDetailsDto(
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
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);
