using FluentValidation;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.JobOrders;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.JobOrders.AcceptJobOrder;

public sealed class AcceptJobOrderCommandValidator : AbstractValidator<AcceptJobOrderCommand>
{
    public AcceptJobOrderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.InvoiceNo).MaximumLength(64);
        RuleFor(x => x.PartialDeliveryNote).MaximumLength(500);
        RuleFor(x => x.PartialDeliveryNote)
            .NotEmpty()
            .When(x => !x.IsCompleteDelivery)
            .WithMessage("Specify the partial delivery quantity/details when delivery is not complete.");
    }
}
