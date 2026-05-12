using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.AddIssuanceReportLines;

public sealed class AddIssuanceReportLinesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<AddIssuanceReportLinesCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(AddIssuanceReportLinesCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIssuanceReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Issuance report '{cmd.ReportId}' not found.");

        var lineIds = cmd.AccountabilityLineIds.Distinct().ToList();
        if (lineIds.Count == 0) return IssuanceMapper.ToDto(report);

        // Pull source accountability lines and their parent accountabilities for snapshotting.
        var accLines = await db.PropertyAccountabilities
            .SelectMany(a => a.Lines.Select(l => new { Accountability = a, Line = l }))
            .Where(x => lineIds.Contains(x.Line.Id))
            .ToListAsync(ct).ConfigureAwait(false);

        var missing = lineIds.Except(accLines.Select(x => x.Line.Id)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"Accountability lines not found: {string.Join(", ", missing)}");

        foreach (var x in accLines)
        {
            report.AddLine(
                x.Accountability.Id,
                x.Line.Id,
                x.Line.AssetRegistryId,
                x.Line.Snapshot,
                x.Line.SnapshotResponsibilityCenterCode,
                x.Line.Snapshot.UnitCost);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}
