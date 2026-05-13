using FluentValidation;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.SubmitPhysicalCountSession;

public sealed class SubmitPhysicalCountSessionCommandValidator : AbstractValidator<SubmitPhysicalCountSessionCommand>
{
    public SubmitPhysicalCountSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

