using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.DeleteFundingSourceCode;

public sealed class DeleteFundingSourceCodeCommandValidator : AbstractValidator<DeleteFundingSourceCodeCommand>
{
    public DeleteFundingSourceCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
