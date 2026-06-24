using AMIS.Framework.Core.Context;
using AMIS.Modules.Chat.Contracts.v1.Channels;
using AMIS.Modules.Chat.Contracts.v1.DTOs;
using AMIS.Modules.Chat.Data;
using AMIS.Modules.Chat.Domain;
using AMIS.Modules.Chat.Features.v1;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.Chat.Features.v1.Channels.FindOrCreateDm;

public sealed class FindOrCreateDmCommandHandler : ICommandHandler<FindOrCreateDmCommand, ChannelDto>
{
    private readonly ChatDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public FindOrCreateDmCommandHandler(ChatDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ChannelDto> Handle(FindOrCreateDmCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId().ToString();
        var tenantId = _currentUser.GetTenant()
            ?? throw new InvalidOperationException("A tenant context is required to open a direct message.");

        var otherUserId = command.OtherUserId?.Trim();
        if (string.IsNullOrWhiteSpace(otherUserId) || string.Equals(otherUserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(command.OtherUserId), "A valid, different user is required to open a direct message.")
            ]);
        }

        var directKey = ChatChannel.BuildDirectKey(userId, otherUserId);

        var existing = await _dbContext.Channels
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.DirectKey == directKey, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing.ToDto();
        }

        var channel = ChatChannel.CreateDirect(tenantId, userId, otherUserId, userId);
        _dbContext.Channels.Add(channel);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return channel.ToDto();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Two users opened the same DM concurrently — both missed the lookup and inserted. The loser
            // re-selects the winner's row rather than surface a 500 or create a duplicate DM.
            var winner = await _dbContext.Channels
                .AsNoTracking()
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.DirectKey == directKey, cancellationToken)
                .ConfigureAwait(false);

            if (winner is null)
            {
                throw;
            }

            return winner.ToDto();
        }
    }
}
