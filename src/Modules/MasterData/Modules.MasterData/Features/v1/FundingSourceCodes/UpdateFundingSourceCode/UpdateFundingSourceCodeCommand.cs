using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.UpdateFundingSourceCode;

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
    bool IsActive = true) : ICommand;
