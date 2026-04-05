using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.OrganizationProfile;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;

public sealed class UpsertOrganizationProfileCommandHandler(MasterDataDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpsertOrganizationProfileCommand, OrganizationProfileDto>
{
    public async ValueTask<OrganizationProfileDto> Handle(
        UpsertOrganizationProfileCommand command, CancellationToken cancellationToken)
    {
        var existing = await db.OrganizationProfiles
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var tenantId = currentUser.GetTenant() ?? string.Empty;
            existing = Domain.OrganizationProfile.Create(
                tenantId, command.Name, command.ShortName, command.Address, command.LogoUrl);
            existing.CreatedBy = currentUser.GetUserId().ToString();
            db.OrganizationProfiles.Add(existing);
        }
        else
        {
            existing.Update(command.Name, command.ShortName, command.Address, command.LogoUrl);
            existing.LastModifiedBy = currentUser.GetUserId().ToString();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OrganizationProfileDto(existing.Id, existing.Name, existing.ShortName, existing.Address, existing.LogoUrl);
    }
}
