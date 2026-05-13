using FluentValidation;
using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.UpdatePpmp;

public sealed class UpdatePpmpCommandValidator : AbstractValidator<UpdatePpmpCommand>
{
    public UpdatePpmpCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.OfficeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EndUserUnit).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PreparedById).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new PpmpItemRequestValidator());
    }
}

