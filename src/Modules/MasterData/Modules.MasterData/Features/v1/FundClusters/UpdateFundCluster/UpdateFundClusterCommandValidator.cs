using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.UpdateFundCluster;

public sealed class UpdateFundClusterCommandValidator : AbstractValidator<UpdateFundClusterCommand>
{
    public UpdateFundClusterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(16).WithMessage("Code must not exceed 16 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250).WithMessage("Name must not exceed 250 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}
