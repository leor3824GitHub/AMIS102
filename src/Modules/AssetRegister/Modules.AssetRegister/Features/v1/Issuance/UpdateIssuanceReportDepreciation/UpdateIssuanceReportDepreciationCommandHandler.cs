using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.UpdateIssuanceReportDepreciation;

public sealed class UpdateIssuanceReportDepreciationCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<UpdateIssuanceReportDepreciationCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(
        UpdateIssuanceReportDepreciationCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var report = await db.PropertyIssuanceReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Issuance report '{cmd.ReportId}' not found.");

        if (report.ReportType != IssuanceReportType.PPEIR)
            throw new CustomException("Depreciation may only be updated on PPEIR reports.", [], System.Net.HttpStatusCode.UnprocessableEntity);

        foreach (var entry in cmd.Lines)
            report.SetLineDepreciation(entry.LineId, entry.AccumulatedDepreciation, entry.BookValue);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}
