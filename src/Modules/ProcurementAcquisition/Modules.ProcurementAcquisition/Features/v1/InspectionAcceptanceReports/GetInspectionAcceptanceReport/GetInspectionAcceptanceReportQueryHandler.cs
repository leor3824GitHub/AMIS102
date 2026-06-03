using AMIS.Modules.ProcurementAcquisition.Contracts.v1.InspectionAcceptanceReports;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.InspectionAcceptanceReports.GetInspectionAcceptanceReport;

public sealed class GetInspectionAcceptanceReportQueryHandler(
    ProcurementDbContext dbContext,
    IMediator mediator) : IQueryHandler<GetInspectionAcceptanceReportQuery, InspectionAcceptanceReportDto?>
{
    public async ValueTask<InspectionAcceptanceReportDto?> Handle(GetInspectionAcceptanceReportQuery query, CancellationToken cancellationToken)
    {
        var iar = await dbContext.InspectionAcceptanceReports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken)
            .ConfigureAwait(false);

        if (iar is null) return null;

        var poNumber = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.Id == iar.PurchaseOrderId)
            .Select(x => x.PoNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false) ?? string.Empty;

        var (inspectorName, custodianName) = await InspectionAcceptanceReportMapper
            .ResolveEmployeeNamesAsync(iar.InspectedById, iar.ReceivedById, mediator, cancellationToken)
            .ConfigureAwait(false);

        var hasSignedCopy = await dbContext.SignedDocuments
            .AsNoTracking()
            .AnyAsync(sd => sd.DocumentType == ProcurementDocumentType.InspectionAcceptanceReport && sd.DocumentId == iar.Id, cancellationToken)
            .ConfigureAwait(false);

        return InspectionAcceptanceReportMapper.ToDto(iar, poNumber, inspectorName, custodianName, hasSignedCopy);
    }
}
