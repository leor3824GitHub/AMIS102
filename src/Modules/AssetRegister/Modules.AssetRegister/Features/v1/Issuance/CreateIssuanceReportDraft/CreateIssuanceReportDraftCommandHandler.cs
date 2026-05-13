using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReportDraft;

public sealed class CreateIssuanceReportDraftCommandHandler(
    AssetRegisterDbContext db,
    IIssuanceReportNumberGenerator numbers)
    : ICommandHandler<CreateIssuanceReportDraftCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(CreateIssuanceReportDraftCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;
        var reportNo = await numbers.NextAsync(cmd.ReportType, cmd.PeriodFromDate, ct).ConfigureAwait(false);

        var preparedBy = EmployeeRef.Create(cmd.PreparedBy.EmployeeId, cmd.PreparedBy.PrintedName, cmd.PreparedBy.Designation);
        var report = PropertyIssuanceReport.CreateDraft(
            tenantId, reportNo, cmd.ReportType, cmd.FundCluster,
            cmd.PeriodFromDate, cmd.PeriodToDate, preparedBy);

        db.PropertyIssuanceReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}

