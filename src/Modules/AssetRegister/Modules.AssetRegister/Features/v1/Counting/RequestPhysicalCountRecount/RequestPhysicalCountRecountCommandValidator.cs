using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.RequestPhysicalCountRecount;

public sealed class RequestPhysicalCountRecountCommandValidator : AbstractValidator<RequestPhysicalCountRecountCommand>
{
    public RequestPhysicalCountRecountCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Physical count session ID is required.");
        RuleFor(x => x.EntryId).NotEmpty().WithMessage("Entry ID is required.");
        RuleFor(x => x.Reason).MaximumLength(500).WithMessage("Recount reason must be 500 characters or fewer.");
    }
}
