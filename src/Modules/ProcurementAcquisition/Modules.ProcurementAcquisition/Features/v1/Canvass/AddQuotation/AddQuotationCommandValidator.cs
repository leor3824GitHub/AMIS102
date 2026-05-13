using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AddQuotation;

public sealed class AddQuotationCommandValidator : AbstractValidator<AddQuotationCommand>
{
    public AddQuotationCommandValidator()
    {
        RuleFor(x => x.CanvassRequestId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SupplierAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TinNumber).MaximumLength(32);
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}

