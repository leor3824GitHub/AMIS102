using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.ReturnAccountabilityLines;

public sealed class ReturnAccountabilityLinesCommandHandler(AssetRegisterDbContext db)
    : ICommandHandler<ReturnAccountabilityLinesCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(ReturnAccountabilityLinesCommand cmd, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        var inputLineIds = cmd.Lines.Select(l => l.LineId).ToHashSet();
        var unknown = inputLineIds.Except(accountability.Lines.Select(l => l.Id)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException($"Unknown lines on this accountability: {string.Join(", ", unknown)}");

        accountability.ReturnLines(
            cmd.Lines.Select(l => (l.LineId, l.OdometerAtReturn)),
            cmd.ReturnedOn,
            cmd.ConditionAtReturn);

        // Mirror the lifecycle change on each affected asset.
        var assetIds = accountability.Lines
            .Where(l => inputLineIds.Contains(l.Id))
            .Select(l => l.AssetRegistryId).ToList();
        var assets = await db.AssetRegistries.Where(a => assetIds.Contains(a.Id)).ToListAsync(ct).ConfigureAwait(false);
        foreach (var asset in assets)
            asset.ReturnToAvailable();

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}

