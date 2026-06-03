using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Domain.InspectionAcceptanceReports;
using Mediator;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;

internal static class InspectionAcceptanceReportMapper
{
    internal static InspectionAcceptanceReportDto ToDto(
        InspectionAcceptanceReport iar,
        string poNumber,
        string inspectorName = "",
        string custodianName = "",
        bool hasSignedCopy = false) =>
        new(iar.Id,
            iar.IarNumber,
            iar.IarDate,
            iar.PurchaseOrderId,
            poNumber,
            iar.SupplierId,
            iar.SupplierName,
            iar.InspectedById,
            inspectorName,
            iar.ReceivedById,
            custodianName,
            iar.DeliveryReceiptNo,
            iar.DeliveryDate,
            iar.Status,
            iar.Remarks,
            iar.LineItems.Select(li => new InspectionAcceptanceReportLineItemDto(
                li.ItemNo, li.Description, li.TechnicalSpecifications,
                li.Brand, li.Model, li.SerialNo, li.PropertyClassHint,
                li.Unit, li.Quantity, li.UnitCost, li.Amount, li.InspectionRemarks,
                li.StockPropertyNo,
                li.InspectionResult,
                li.InspectedOnUtc,
                li.InspectedById,
                li.CatalogItemId,
                li.UacsObjectCode,
                li.StockNumber)).ToList(),
            iar.TotalAmount,
            iar.CreatedOnUtc,
            iar.CreatedBy,
            iar.LastModifiedOnUtc,
            iar.SubmittedForInspectionOnUtc,
            iar.InspectedOnUtc,
            iar.AcceptedOnUtc,
            iar.CancelledOnUtc,
            hasSignedCopy,
            iar.Category,
            iar.AcceptedById,
            iar.AcceptedByName,
            iar.AcceptedByDesignation);

    internal static async Task<(string InspectorName, string CustodianName)> ResolveEmployeeNamesAsync(
        Guid inspectedById,
        Guid receivedById,
        IMediator mediator,
        CancellationToken ct)
    {
        var ids = new HashSet<Guid>();
        if (inspectedById != Guid.Empty) ids.Add(inspectedById);
        if (receivedById != Guid.Empty) ids.Add(receivedById);
        if (ids.Count == 0) return (string.Empty, string.Empty);

        var map = await mediator.Send(new GetEmployeeReferencesByIdsQuery(ids), ct).ConfigureAwait(false);

        return (FormatName(map, inspectedById), FormatName(map, receivedById));
    }

    private static string FormatName(IReadOnlyDictionary<Guid, EmployeeReferenceDto> map, Guid id) =>
        map.TryGetValue(id, out var emp) ? $"{emp.FirstName} {emp.LastName}".Trim() : string.Empty;
}
