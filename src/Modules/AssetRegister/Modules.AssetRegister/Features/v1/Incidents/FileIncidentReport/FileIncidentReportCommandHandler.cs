using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using FSH.Modules.AssetRegister.Contracts.v1.ValueObjects;
using FSH.Modules.AssetRegister.Data;
using FSH.Modules.AssetRegister.Domain.Incidents;
using FSH.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.FileIncidentReport;

public sealed class FileIncidentReportCommandHandler(
    AssetRegisterDbContext db,
    IIncidentNumberGenerator numbers,
    ICurrentReplacementCostCalculator crc)
    : ICommandHandler<FileIncidentReportCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(FileIncidentReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        var assetIds = cmd.Items.Select(i => i.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        var missing = assetIds.Except(assets.Select(a => a.Id)).ToList();
        if (missing.Count > 0)
            throw new KeyNotFoundException($"Assets not found: {string.Join(", ", missing)}");

        var assetById = assets.ToDictionary(a => a.Id);

        var incidentNo = await numbers.NextAsync(cmd.IncidentDate, ct).ConfigureAwait(false);
        var officer = EmployeeRef.Create(
            cmd.AccountableOfficer.EmployeeId, cmd.AccountableOfficer.PrintedName, cmd.AccountableOfficer.Designation);

        var domainItems = new List<(Guid, AssetSnapshot, decimal, decimal, Guid?)>();
        foreach (var item in cmd.Items)
        {
            var asset = assetById[item.AssetRegistryId];
            var crcValue = await crc.ComputeAsync(asset.Id, cmd.IncidentDate, ct).ConfigureAwait(false);
            domainItems.Add((asset.Id, asset.Snapshot(), asset.UnitCost, crcValue, item.AccountabilityLineId));
        }

        var report = PropertyIncidentReport.File(
            tenantId, incidentNo, cmd.IncidentType, cmd.IncidentDate, cmd.FundCluster,
            cmd.DepartmentOffice, cmd.Circumstances, officer, cmd.AccountableOfficerDesignation,
            domainItems);

        // Mirror lifecycle: each asset → UnderInvestigation; flip linked accountability lines → Lost.
        foreach (var item in cmd.Items)
        {
            var asset = assetById[item.AssetRegistryId];
            asset.MarkUnderInvestigation(report.Id);
        }

        var lineIds = cmd.Items.Where(i => i.AccountabilityLineId.HasValue)
            .Select(i => i.AccountabilityLineId!.Value).ToList();
        if (lineIds.Count > 0)
        {
            var accountabilities = await db.PropertyAccountabilities
                .Include(a => a.Lines)
                .Where(a => a.Lines.Any(l => lineIds.Contains(l.Id)))
                .ToListAsync(ct).ConfigureAwait(false);
            foreach (var acc in accountabilities)
            {
                foreach (var l in acc.Lines.Where(l => lineIds.Contains(l.Id)))
                    acc.ReportLineLost(l.Id, report.Id);
            }
        }

        db.PropertyIncidentReports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}
