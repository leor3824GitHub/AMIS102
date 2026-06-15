using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents;

public sealed class RecordIncidentRecoveryCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordIncidentRecoveryCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(RecordIncidentRecoveryCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.RecordRecovery(cmd.ItemId, cmd.RecoveredOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{item.AssetRegistryId}' not found.");
        asset.MarkRecovered(report.Id);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class RecordIncidentSettlementCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<RecordIncidentSettlementCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(RecordIncidentSettlementCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.RecordSettlement(cmd.ItemId, cmd.Amount, cmd.SettledOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{item.AssetRegistryId}' not found.");
        await freezeGuard.EnsureMovementAllowedAsync([asset], cancellationToken).ConfigureAwait(false);
        asset.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class GrantIncidentReliefCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<GrantIncidentReliefCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(GrantIncidentReliefCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.GrantRelief(cmd.ItemId, cmd.GrantedOn, cmd.DecisionRef);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{item.AssetRegistryId}' not found.");
        await freezeGuard.EnsureMovementAllowedAsync([asset], cancellationToken).ConfigureAwait(false);
        asset.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class DerecognizeIncidentItemCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<DerecognizeIncidentItemCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(DerecognizeIncidentItemCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.MarkDerecognized(cmd.ItemId, cmd.RecordedOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Asset '{item.AssetRegistryId}' not found.");
        await freezeGuard.EnsureMovementAllowedAsync([asset], cancellationToken).ConfigureAwait(false);
        asset.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class CloseIncidentReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CloseIncidentReportCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(CloseIncidentReportCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, cancellationToken).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        report.Close();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

