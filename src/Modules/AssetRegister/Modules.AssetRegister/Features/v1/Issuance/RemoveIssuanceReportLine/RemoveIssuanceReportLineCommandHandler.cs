using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance.RemoveIssuanceReportLine;

public sealed class RemoveIssuanceReportLineCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<RemoveIssuanceReportLineCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(RemoveIssuanceReportLineCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIssuanceReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Issuance report '{cmd.ReportId}' not found.");

        report.RemoveLine(cmd.LineId);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}
