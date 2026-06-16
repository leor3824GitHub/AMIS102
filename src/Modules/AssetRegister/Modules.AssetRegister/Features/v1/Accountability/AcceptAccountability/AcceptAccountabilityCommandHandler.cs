using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Data;
using AMIS.Modules.AssetRegister.Features.v1.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.AssetRegister.Features.v1.Accountability.AcceptAccountability;

public sealed class AcceptAccountabilityCommandHandler(
    AssetRegisterDbContext db, ICurrentUser currentUser, IMediator mediator)
    : ICommandHandler<AcceptAccountabilityCommand, PropertyAccountabilityDto>
{
    public async ValueTask<PropertyAccountabilityDto> Handle(AcceptAccountabilityCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        var accountability = await db.PropertyAccountabilities
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == cmd.AccountabilityId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Accountability '{cmd.AccountabilityId}' not found.");

        // Ownership gate: only the named recipient may accept — even with the Acknowledge permission.
        var employee = await CurrentEmployeeResolver.TryResolveAsync(currentUser, mediator, cancellationToken).ConfigureAwait(false)
            ?? throw new ForbiddenException("No employee profile is linked to your account.");
        if (accountability.ReceivedBy.EmployeeId != employee.Id)
            throw new ForbiddenException("You can only accept accountabilities issued to you.");

        accountability.Accept(cmd.AcceptedOn);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}
