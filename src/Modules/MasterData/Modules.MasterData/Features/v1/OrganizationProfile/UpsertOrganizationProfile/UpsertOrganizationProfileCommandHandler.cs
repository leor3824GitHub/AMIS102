using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;

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
                tenantId, command.Name, command.ShortName, command.Address, command.LogoUrl, command.AnnexECode);
            existing.CreatedBy = currentUser.GetUserId().ToString();
            db.OrganizationProfiles.Add(existing);
        }
        else
        {
            existing.Update(command.Name, command.ShortName, command.Address, command.LogoUrl, command.AnnexECode);
            existing.LastModifiedBy = currentUser.GetUserId().ToString();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OrganizationProfileDto(existing.Id, existing.Name, existing.ShortName, existing.Address, existing.LogoUrl, existing.AnnexECode);
    }
}

