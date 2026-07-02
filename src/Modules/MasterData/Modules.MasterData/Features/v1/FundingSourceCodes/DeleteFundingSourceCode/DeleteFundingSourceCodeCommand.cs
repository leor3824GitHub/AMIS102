using Mediator;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.DeleteFundingSourceCode;

public sealed record DeleteFundingSourceCodeCommand(Guid Id) : ICommand;
