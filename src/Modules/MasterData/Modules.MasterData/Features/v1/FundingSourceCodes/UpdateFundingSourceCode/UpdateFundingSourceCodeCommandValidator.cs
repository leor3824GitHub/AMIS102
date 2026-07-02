using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.FundingSourceCodes.UpdateFundingSourceCode;

public sealed class UpdateFundingSourceCodeCommandValidator : AbstractValidator<UpdateFundingSourceCodeCommand>
{
    public UpdateFundingSourceCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");

        RuleFor(x => x.FundClusterCode)
            .NotEmpty().WithMessage("Fund cluster code is required.")
            .MaximumLength(16).WithMessage("Fund cluster code must not exceed 16 characters.");

        RuleFor(x => x.FinancingSource).MaximumLength(250);
        RuleFor(x => x.Authorization).MaximumLength(250);
        RuleFor(x => x.FundCategory).MaximumLength(250);
        RuleFor(x => x.FundSubCategory).MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DepartmentName).MaximumLength(250);
        RuleFor(x => x.AgencyName).MaximumLength(250);
    }
}
