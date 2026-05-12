using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents;

public sealed class RecordIncidentRecoveryCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordIncidentRecoveryCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(RecordIncidentRecoveryCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.RecordRecovery(cmd.ItemId, cmd.RecoveredOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, ct).ConfigureAwait(false);
        asset?.MarkRecovered(report.Id);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class RecordIncidentSettlementCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RecordIncidentSettlementCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(RecordIncidentSettlementCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.RecordSettlement(cmd.ItemId, cmd.Amount, cmd.SettledOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, ct).ConfigureAwait(false);
        asset?.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class GrantIncidentReliefCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<GrantIncidentReliefCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(GrantIncidentReliefCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.GrantRelief(cmd.ItemId, cmd.GrantedOn, cmd.DecisionRef);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, ct).ConfigureAwait(false);
        asset?.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class DerecognizeIncidentItemCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<DerecognizeIncidentItemCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(DerecognizeIncidentItemCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        var item = report.Items.FirstOrDefault(i => i.Id == cmd.ItemId)
            ?? throw new KeyNotFoundException($"Incident item '{cmd.ItemId}' not found.");
        report.MarkDerecognized(cmd.ItemId, cmd.RecordedOn);

        var asset = await db.AssetRegistries.FirstOrDefaultAsync(a => a.Id == item.AssetRegistryId, ct).ConfigureAwait(false);
        asset?.Dispose(report.Id, Contracts.v1.DisposalMethod.Other);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

public sealed class CloseIncidentReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<CloseIncidentReportCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(CloseIncidentReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        report.Close();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}
