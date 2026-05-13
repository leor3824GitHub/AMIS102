using System.Globalization;
using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Data.Services;
using AMIS.Modules.AssetRegister.Domain.Counting;
using Mediator;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting.StartPhysicalCount;

public sealed class StartPhysicalCountCommandHandler(
    AssetRegisterDbContext db, CounterAllocator allocator)
    : ICommandHandler<StartPhysicalCountCommand, PhysicalCountSessionDto>
{
    public async ValueTask<PhysicalCountSessionDto> Handle(StartPhysicalCountCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var tenantId = db.TenantInfo?.Identifier ?? string.Empty;

        // Auto-mint a session code (PCS-YYYY-NNNN) when caller passes blank.
        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? $"PCS-{cmd.AsAt.Year:D4}-{(await allocator.NextSerialAsync(tenantId, cmd.AsAt.Year, 1, "PCS", ct).ConfigureAwait(false)).ToString("D4", CultureInfo.InvariantCulture)}"
            : cmd.Code;

        var conductedBy = cmd.ConductedBy
            .Select(e => EmployeeRef.Create(e.EmployeeId, e.PrintedName, e.Designation));

        var session = PhysicalCountSession.Start(
            tenantId, code, cmd.Scope, cmd.FundCluster, cmd.AsAt, cmd.StartedOn, conductedBy, cmd.Remarks);

        db.PhysicalCountSessions.Add(session);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return CountingMapper.ToDto(session);
    }
}

