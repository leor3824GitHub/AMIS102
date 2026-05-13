using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.Expendable.Domain.Requests;

namespace AMIS.Modules.Expendable.Features.v1.Requests;

internal static class SupplyRequestMapper
{
    internal static SupplyRequestDto ToSupplyRequestDto(this SupplyRequest request) =>
        new(
            request.Id,
            request.RequestNumber,
            request.EmployeeId,
            request.DepartmentId,
            request.RequestDate,
            request.NeededByDate,
            request.Status.ToString(),
            request.BusinessJustification,
            request.RejectionReason,
            request.ApprovedBy,
            request.ApprovedOnUtc,
            request.FulfilledOnUtc,
            request.WarehouseLocationId,
            request.Items.Select(x => new SupplyRequestItemDto(
                x.ProductId,
                x.RequestedQuantity,
                x.ApprovedQuantity,
                x.FulfilledQuantity,
                x.Notes)).ToList(),
            request.CreatedOnUtc,
            request.CreatedBy);
}


