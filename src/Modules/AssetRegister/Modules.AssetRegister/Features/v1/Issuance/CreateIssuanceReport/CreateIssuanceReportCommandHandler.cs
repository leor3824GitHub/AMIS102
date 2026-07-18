using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Assets;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using AMIS.Modules.AssetRegister.Domain.Services;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.CreateIssuanceReport;

public sealed class CreateIssuanceReportCommandHandler(
    AssetRegisterDbContext db,
    IIssuanceReportNumberGenerator numbers,
    ICountFreezeGuard freezeGuard,
    AssetTransferProjector projector,
    IMediator mediator)
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

        // The approving authority is the organization's approving official — resolved here
        // from the Organization Profile and snapshotted onto the report (not entered per report).
        var org = await mediator.Send(new GetOrganizationProfileQuery(), cancellationToken).ConfigureAwait(false);
        if (org is null || string.IsNullOrWhiteSpace(org.ApprovingOfficialName))
            throw new CustomException(
                "No approving official is set in the Organization Profile. Set it before creating an issuance report.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        var reportNo = await numbers.NextAsync(cmd.ReportType, cmd.Date, cancellationToken).ConfigureAwait(false);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var issuedBy   = EmployeeRef.Create(cmd.IssuedBy.EmployeeId, cmd.IssuedBy.PrintedName, cmd.IssuedBy.Designation);
        var approvedBy = EmployeeRef.Create(
            Guid.Empty,
            org.ApprovingOfficialName.Trim(),
            string.IsNullOrWhiteSpace(org.ApprovingOfficialDesignation) ? null : org.ApprovingOfficialDesignation.Trim());
        var issuedTo   = EmployeeRef.Create(cmd.IssuedTo.EmployeeId, cmd.IssuedTo.PrintedName, cmd.IssuedTo.Designation);

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

        // Inter-agency transfer: the outbound offer is written in the SAME SaveChanges as the report, so the
        // sender's books and the offer can never disagree. Delivery into the receiving tenant is a separate
        // transaction (Finbuckle forbids saving another tenant's rows here), driven off this row by
        // AssetTransferProjectionJob — the offer row is its own outbox.
        if (!string.IsNullOrWhiteSpace(cmd.DestinationTenantId))
        {
            var offer = await BuildTransferOfferAsync(
                cmd.DestinationTenantId, tenantId, org, report, assets, cancellationToken).ConfigureAwait(false);
            db.AssetTransferOffers.Add(offer);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await db.Entry(report).Collection(r => r.Lines).LoadAsync(cancellationToken).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }

    /// <summary>
    /// Snapshots the assets being transferred onto an outbound offer.
    /// <para>
    /// Book values come from <see cref="AssetRegistry"/>, not from the issuance report lines. The registry is
    /// the system of record — the depreciation engine posts to it monthly — whereas a line's
    /// <c>AccumulatedDepreciation</c> is an Accounting-entered figure for the printed form that is still null
    /// at post time. Carrying the asset's own <c>DepreciatedThrough</c> alongside the amount is what lets the
    /// receiving agency resume the schedule instead of replaying it from the original acquisition date.
    /// </para>
    /// </summary>
    private async Task<Domain.Transfers.AssetTransferOffer> BuildTransferOfferAsync(
        string destinationTenantId,
        string tenantId,
        OrganizationProfileDto org,
        PropertyIssuanceReport report,
        IReadOnlyCollection<AssetRegistry> assets,
        CancellationToken cancellationToken)
    {
        if (!IsTransferNature(report.Nature))
            throw new CustomException(
                $"A destination agency may only be set on a transfer issuance (nature Transfer to C.O./R.O./P.O.). " +
                $"This report's nature is '{report.Nature}'.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        if (report.ReportType != IssuanceReportType.PPEIR)
            throw new CustomException(
                "Linked inter-agency transfers are currently supported for PPE (PPEIR → PPERR) only. " +
                "Post the SMIR without a destination agency and have the receiving agency key its SMRR.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        // Never take a tenant id from the client on trust — it must exist and be active in the registry.
        var destination = await projector
            .ResolveActiveTenantAsync(destinationTenantId, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException(
                $"Destination agency '{destinationTenantId}' is unknown or deactivated.",
                [], System.Net.HttpStatusCode.UnprocessableEntity);

        if (string.Equals(destination.Identifier, tenantId, StringComparison.OrdinalIgnoreCase))
            throw new CustomException(
                "An agency cannot transfer property to itself.", [], System.Net.HttpStatusCode.UnprocessableEntity);

        var offer = Domain.Transfers.AssetTransferOffer.CreateOutbound(
            tenantId,
            Guid.NewGuid(),
            org.Name,
            destination.Identifier,
            destination.Name ?? destination.Identifier,
            report.Id,
            report.ReportNo,
            report.ReportType);

        foreach (var asset in assets)
        {
            offer.AddLine(
                asset.PropertyNo.Value,
                asset.Description,
                asset.SerialNo,
                asset.Brand,
                asset.Model,
                asset.UnitCost,
                asset.AcquisitionDate,
                asset.AccumulatedDepreciation,
                asset.DepreciatedThrough,
                asset.CarryingAmount,
                asset.UacsObjectCode);
        }

        return offer;
    }

    private static bool IsTransferNature(IssuanceNature nature) =>
        nature is IssuanceNature.TransferCO or IssuanceNature.TransferRO or IssuanceNature.TransferPO;
}
