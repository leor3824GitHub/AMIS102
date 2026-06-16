using System.Net;
using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.AssetRegister.Contracts.v1;
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

        // Cross-client race: the same person may accept this document from the MAUI app and the
        // web UI at the same time. Whoever loses the race gets a clear 409 instead of a generic 500,
        // so the UI can tell them it was already accepted (and refresh) rather than swallowing it.
        if (accountability.Status != AccountabilityStatus.PendingAcceptance)
        {
            var label = accountability.AccountabilityType == AccountabilityType.SE_ICS ? "ICS" : "PAR";
            var message = accountability.Status == AccountabilityStatus.Active && accountability.AcceptedOn is { } acceptedOn
                ? $"{label} '{accountability.DocumentNo}' was already accepted on {acceptedOn:yyyy-MM-dd}. Refresh to see its current status."
                : $"{label} '{accountability.DocumentNo}' can no longer be accepted because it is {accountability.Status}. Refresh to see its current status.";
            throw new CustomException(message, (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        accountability.Accept(cmd.AcceptedOn);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return AccountabilityMapper.ToDto(accountability);
    }
}
