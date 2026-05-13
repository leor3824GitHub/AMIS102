using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using FSH.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.NotarizeIncidentReport;

public sealed class NotarizeIncidentReportCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<NotarizeIncidentReportCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(NotarizeIncidentReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        report.Notarize(cmd.NotarizedOn, cmd.DocNo, cmd.PageNo, cmd.BookNo, cmd.SeriesOf);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}
