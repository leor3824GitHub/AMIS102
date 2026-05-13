using AMIS.Modules.AssetRegister.Contracts.v1.Incidents;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Incidents.NotifyIncidentPolice;

public sealed class NotifyIncidentPoliceCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<NotifyIncidentPoliceCommand, PropertyIncidentReportDto>
{
    public async ValueTask<PropertyIncidentReportDto> Handle(NotifyIncidentPoliceCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIncidentReports.Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == cmd.IncidentReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Incident report '{cmd.IncidentReportId}' not found.");
        report.NotifyPolice(cmd.Station, cmd.NotifiedOn, cmd.BlotterRef);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IncidentMapper.ToDto(report);
    }
}

