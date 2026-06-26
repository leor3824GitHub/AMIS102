using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.UpdateJobOrder;

public sealed class UpdateJobOrderCommandValidator : AbstractValidator<UpdateJobOrderCommand>
{
    public UpdateJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.JobRequestNo).MaximumLength(64);
        RuleFor(x => x.RequisitioningOffice).MaximumLength(256);
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.SupplierName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SupplierAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SupplierTin).MaximumLength(32);
        RuleFor(x => x.PlaceOfDelivery).NotEmpty().MaximumLength(256);
        RuleFor(x => x.DeliveryTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.PaymentTerm).NotEmpty().MaximumLength(256);
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(x => x.Unit).MaximumLength(64);
            li.RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
            li.RuleFor(x => x.Quantity).GreaterThan(0);
            li.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
