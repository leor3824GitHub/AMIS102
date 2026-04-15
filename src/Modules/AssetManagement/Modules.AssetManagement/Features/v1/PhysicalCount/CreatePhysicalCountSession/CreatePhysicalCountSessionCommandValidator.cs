using FluentValidation;
using FSH.Modules.AssetManagement.Domain;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;

public sealed class CreatePhysicalCountSessionCommandValidator : AbstractValidator<CreatePhysicalCountSessionCommand>
{
    public CreatePhysicalCountSessionCommandValidator()
    {
        RuleFor(x => x.SessionNo).NotEmpty().MaximumLength(32);
        RuleFor(x => x.CountDate).NotEmpty();
        RuleFor(x => x.StationOffice).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Scope).IsInEnum();
        RuleFor(x => x.PreparedByEmployeeId).NotEmpty();
        RuleFor(x => x.CertifiedByEmployeeId).NotEmpty();
        RuleFor(x => x.ApprovedByEmployeeId).NotEmpty();
    }
}
