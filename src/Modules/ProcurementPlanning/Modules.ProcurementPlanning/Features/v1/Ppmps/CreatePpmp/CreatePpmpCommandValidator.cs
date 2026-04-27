using FluentValidation;
using FSH.Modules.ProcurementPlanning.Contracts.v1.Ppmps;

namespace FSH.Modules.ProcurementPlanning.Features.v1.Ppmps.CreatePpmp;

public sealed class CreatePpmpCommandValidator : AbstractValidator<CreatePpmpCommand>
{
    public CreatePpmpCommandValidator()
    {
        RuleFor(x => x.FiscalYear).InclusiveBetween(2000, 2100);
        RuleFor(x => x.OfficeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EndUserUnit).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PreparedById).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item is required.");
        RuleForEach(x => x.Items).SetValidator(new PpmpItemRequestValidator());
    }
}

internal sealed class PpmpItemRequestValidator : AbstractValidator<PpmpItemRequest>
{
    public PpmpItemRequestValidator()
    {
        RuleFor(x => x.GeneralDescription).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.EstimatedBudget).GreaterThan(0);
        RuleFor(x => x.ProcurementStart).NotEmpty().Matches(@"^\d{2}/\d{4}$").WithMessage("ProcurementStart must be MM/YYYY.");
        RuleFor(x => x.ProcurementEnd).NotEmpty().Matches(@"^\d{2}/\d{4}$").WithMessage("ProcurementEnd must be MM/YYYY.");
        RuleFor(x => x.ExpectedDelivery).NotEmpty().Matches(@"^\d{2}/\d{4}$").WithMessage("ExpectedDelivery must be MM/YYYY.");
        RuleFor(x => x.SourceOfFunds).NotEmpty().MaximumLength(256);
    }
}
