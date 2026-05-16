using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.GetOrganizationProfile;

public sealed class GetOrganizationProfileQueryHandler(MasterDataDbContext db)
    : IQueryHandler<GetOrganizationProfileQuery, OrganizationProfileDto?>
{
    public async ValueTask<OrganizationProfileDto?> Handle(
        GetOrganizationProfileQuery query, CancellationToken cancellationToken)
    {
        var entity = await db.OrganizationProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? null
            : new OrganizationProfileDto(
                entity.Id, entity.Name, entity.ShortName, entity.Address, entity.LogoUrl, entity.AnnexECode,
                entity.RegionalManagerId, entity.RegionalManagerName, entity.RegionalManagerDesignation,
                entity.AssistantRegionalManagerId, entity.AssistantRegionalManagerName, entity.AssistantRegionalManagerDesignation,
                entity.AccountantId, entity.AccountantName, entity.AccountantDesignation,
                entity.SupervisingAdminOfficerId, entity.SupervisingAdminOfficerName, entity.SupervisingAdminOfficerDesignation);
    }
}

