using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Counting;

namespace FSH.Modules.AssetRegister.Features.v1.Counting.ReconcilePhysicalCount;

public sealed class ReconcilePhysicalCountCommandValidator : AbstractValidator<ReconcilePhysicalCountCommand>
{
    public ReconcilePhysicalCountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
    }
}
