using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReport;

public sealed class CreateIssuanceReportCommandHandler(
    AssetRegisterDbContext db,
    IIssuanceReportNumberGenerator numbers,
    ICountFreezeGuard freezeGuard)
    : ICommandHandler<CreateIssuanceReportCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(CreateIssuanceReportCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var distinctIds = cmd.AssetRegistryIds.Distinct().ToList();
        if (distinctIds.Count != cmd.AssetRegistryIds.Count)
            throw new CustomException("Duplicate asset IDs in the request.", [], System.Net.HttpStatusCode.Conflict);

        var assets = await db.AssetRegistries
            .Where(a => distinctIds.Contains(a.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (assets.Count != distinctIds.Count)
        {
            var missing = distinctIds.Except(assets.Select(a => a.Id));
            throw new KeyNotFoundException($"Assets not found: {string.Join(", ", missing)}");
        }

        var expectedAssetType = cmd.ReportType == IssuanceReportType.SMIR ? AssetType.SE : AssetType.PPE;
        var wrongType = assets.Where(a => a.AssetType != expectedAssetType).ToList();
        if (wrongType.Count > 0)
            throw new CustomException(
                $"Report type {cmd.ReportType} requires all assets to be {expectedAssetType}. " +
                $"Incompatible assets: {string.Join(", ", wrongType.Select(a => a.PropertyNo.Value))}",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        var notAvailable = assets.Where(a => a.LifecycleState != LifecycleState.Available).ToList();
        if (notAvailable.Count > 0)
            throw new CustomException(
                $"The following assets have an active accountability or are not available for issuance. " +
                $"They must be returned first: {string.Join(", ", notAvailable.Select(a => a.PropertyNo.Value))}",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        await freezeGuard.EnsureMovementAllowedAsync(assets, cancellationToken).ConfigureAwait(false);

        var reportNo = await numbers.NextAsync(cmd.ReportType, cmd.Date, cancellationToken).ConfigureAwait(false);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var issuedBy   = EmployeeRef.Create(cmd.IssuedBy.EmployeeId,   cmd.IssuedBy.PrintedName,   cmd.IssuedBy.Designation);
        var approvedBy = EmployeeRef.Create(cmd.ApprovedBy.EmployeeId, cmd.ApprovedBy.PrintedName, cmd.ApprovedBy.Designation);
        var issuedTo   = EmployeeRef.Create(cmd.IssuedTo.EmployeeId,   cmd.IssuedTo.PrintedName,   cmd.IssuedTo.Designation);

        var report = PropertyIssuanceReport.Create(
            tenantId,
            reportNo,
            cmd.ReportType,
            cmd.FundCluster,
            cmd.Date,
            cmd.Nature,
            issuedBy,
            approvedBy,
            issuedTo,
            cmd.IssuedToOfficeAddress,
            cmd.Remarks);

        foreach (var asset in assets)
        {
            report.AddLine(asset.Id, asset.Snapshot(), asset.UnitCost);
            asset.MarkTransferredOut(report.Id, report.ReportNo, report.ReportType);
        }

        report.MarkIssued();

        db.PropertyIssuanceReports.Add(report);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await db.Entry(report).Collection(r => r.Lines).LoadAsync(cancellationToken).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}
