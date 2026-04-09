using FluentValidation;
using FSH.Modules.ProcurementAcquisition.Contracts.v1.Canvass;

namespace FSH.Modules.ProcurementAcquisition.Features.v1.Canvass.UpdateQuotation;

public sealed class UpdateQuotationCommandValidator : AbstractValidator<UpdateQuotationCommand>
{
    public UpdateQuotationCommandValidator()
    {
        RuleFor(x => x.QuotationId).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SupplierAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TinNumber).MaximumLength(32);
        RuleFor(x => x.LineItems).NotEmpty();
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Unit).NotEmpty().MaximumLength(64);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
