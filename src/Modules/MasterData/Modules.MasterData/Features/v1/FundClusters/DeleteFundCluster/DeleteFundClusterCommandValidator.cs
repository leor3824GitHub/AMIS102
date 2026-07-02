using FluentValidation;

namespace AMIS.Modules.MasterData.Features.v1.FundClusters.DeleteFundCluster;

public sealed class DeleteFundClusterCommandValidator : AbstractValidator<DeleteFundClusterCommand>
{
    public DeleteFundClusterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
