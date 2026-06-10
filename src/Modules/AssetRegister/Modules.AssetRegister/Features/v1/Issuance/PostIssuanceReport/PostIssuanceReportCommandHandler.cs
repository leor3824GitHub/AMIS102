using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Domain.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.PostIssuanceReport;

public sealed class PostIssuanceReportCommandHandler(AssetRegisterDbContext db, ICountFreezeGuard freezeGuard)
    : ICommandHandler<PostIssuanceReportCommand, PropertyIssuanceReportDto>
{
    public async ValueTask<PropertyIssuanceReportDto> Handle(PostIssuanceReportCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var report = await db.PropertyIssuanceReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == cmd.ReportId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Issuance report '{cmd.ReportId}' not found.");

        var certifiedBy = EmployeeRef.Create(cmd.CertifiedBy.EmployeeId, cmd.CertifiedBy.PrintedName, cmd.CertifiedBy.Designation);
        var postedBy = EmployeeRef.Create(cmd.PostedBy.EmployeeId, cmd.PostedBy.PrintedName, cmd.PostedBy.Designation);
        report.Post(certifiedBy, postedBy, cmd.PostedOn);

        var assetIds = report.Lines.Select(l => l.AssetRegistryId).Distinct().ToList();
        var assets = await db.AssetRegistries
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync(ct).ConfigureAwait(false);
        await freezeGuard.EnsureMovementAllowedAsync(assets, ct).ConfigureAwait(false);
        foreach (var asset in assets)
            asset.MarkTransferredOut(report.Id, report.ReportNo, report.ReportType);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return IssuanceMapper.ToDto(report);
    }
}

