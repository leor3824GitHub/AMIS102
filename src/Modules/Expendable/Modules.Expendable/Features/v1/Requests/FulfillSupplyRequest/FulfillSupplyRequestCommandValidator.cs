using FluentValidation;
using AMIS.Modules.Expendable.Contracts.v1.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests.FulfillSupplyRequest;

public sealed class FulfillSupplyRequestCommandValidator : AbstractValidator<FulfillSupplyRequestCommand>
{
    public FulfillSupplyRequestCommandValidator()
    {
        RuleFor(x => x.SupplyRequestId).NotEmpty();
        // WarehouseLocationId is optional — defaults to the warehouse set at approval time
    }
}

