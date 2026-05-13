using AMIS.Modules.AssetManagement.Data;
using AMIS.Modules.MasterData.Contracts.v1.References;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetManagement.Features.v1.PPEIssuanceReports.GetPTR;

public sealed class GetPTRQueryHandler(AssetManagementDbContext dbContext, IMediator mediator)
    : IQueryHandler<GetPTRQuery, PTRDto>
{
    public async ValueTask<PTRDto> Handle(GetPTRQuery query, CancellationToken cancellationToken)
    {
        var ppeir = await dbContext.PPEIssuanceReports
            .FirstOrDefaultAsync(x => x.Id == query.PPEIRId, cancellationToken)
            .ConfigureAwait(false);

        if (ppeir is null)
        {
            throw new KeyNotFoundException($"PPE Issuance Report with ID {query.PPEIRId} not found.");
        }

        var items = await (
            from ppeirItem in dbContext.PPEIRItems.Where(x => x.PPEIRId == query.PPEIRId)
            join inv in dbContext.TangibleInventoryItems
                on ppeirItem.TangibleInventoryItemId equals inv.Id
            orderby ppeirItem.ItemNo
            select new PTRItemDto(
                ppeirItem.ItemNo,
                ppeirItem.DateAcquired,
                inv.PropertyNo,
                ppeirItem.PPESpecification,
                ppeirItem.AcquisitionCost,
                null,
                null))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var employeeIds = new[]
        {
            ppeir.IssuedByEmployeeId,
            ppeir.IssuedToEmployeeId,
            ppeir.ApprovedByEmployeeId,
            ppeir.ReceivedByEmployeeId
        }
        .Distinct()
        .ToList();

        var employeeMap = await mediator
            .Send(new GetEmployeeReferencesByIdsQuery(employeeIds), cancellationToken)
            .ConfigureAwait(false);

        employeeMap.TryGetValue(ppeir.IssuedByEmployeeId, out var fromOfficer);
        employeeMap.TryGetValue(ppeir.IssuedToEmployeeId, out var toOfficer);
        employeeMap.TryGetValue(ppeir.ApprovedByEmployeeId, out var approvedByOfficer);
        employeeMap.TryGetValue(ppeir.ReceivedByEmployeeId, out var receivedByOfficer);

        return new PTRDto(
            PTRNo:                   ppeir.PPEIRNo,
            Date:                    ppeir.Date,
            FromAccountableOfficerId: ppeir.IssuedByEmployeeId,
            FromAccountableOfficerName: BuildEmployeeDisplayName(fromOfficer, ppeir.IssuedByEmployeeId),
            FromAccountableOfficerPosition: fromOfficer?.PositionName,
            FromAccountableOfficerOffice: fromOfficer?.OfficeName,
            ToAccountableOfficerId:  ppeir.IssuedToEmployeeId,
            ToAccountableOfficerName: BuildEmployeeDisplayName(toOfficer, ppeir.IssuedToEmployeeId),
            ToAccountableOfficerPosition: toOfficer?.PositionName,
            ToAccountableOfficerOffice: toOfficer?.OfficeName,
            ToOfficeAddress:         ppeir.IssuedToOfficeAddress,
            TransferType:            ppeir.IssuanceType.ToString(),
            ApprovedByEmployeeId:    ppeir.ApprovedByEmployeeId,
            ApprovedByEmployeeName: BuildEmployeeDisplayName(approvedByOfficer, ppeir.ApprovedByEmployeeId),
            ApprovedByEmployeePosition: approvedByOfficer?.PositionName,
            ApprovedByEmployeeOffice: approvedByOfficer?.OfficeName,
            ReleasedByEmployeeId:    ppeir.IssuedByEmployeeId,
            ReleasedByEmployeeName: BuildEmployeeDisplayName(fromOfficer, ppeir.IssuedByEmployeeId),
            ReleasedByEmployeePosition: fromOfficer?.PositionName,
            ReleasedByEmployeeOffice: fromOfficer?.OfficeName,
            ReceivedByEmployeeId:    ppeir.ReceivedByEmployeeId,
            ReceivedByEmployeeName: BuildEmployeeDisplayName(receivedByOfficer, ppeir.ReceivedByEmployeeId),
            ReceivedByEmployeePosition: receivedByOfficer?.PositionName,
            ReceivedByEmployeeOffice: receivedByOfficer?.OfficeName,
            Items:                   items);
    }

    private static string BuildEmployeeDisplayName(EmployeeReferenceDto? employee, Guid fallbackId)
    {
        if (employee is null)
        {
            return fallbackId.ToString();
        }

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? fallbackId.ToString() : fullName;
    }
}

