using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.GetOrganizationProfile;

public sealed class GetOrganizationProfileQueryHandler(MasterDataDbContext db, AgencyOfficeResolver officeResolver)
    : IQueryHandler<GetOrganizationProfileQuery, OrganizationProfileDto?>
{
    public async ValueTask<OrganizationProfileDto?> Handle(
        GetOrganizationProfileQuery query, CancellationToken cancellationToken)
    {
        var entity = await db.OrganizationProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        // Resolved on read, not only on save: when an administrator re-links the tenant to another office the
        // profile follows immediately, without anyone having to open and re-save it. Falls back to the stored
        // code so an unlinked tenant keeps whatever it was set up with.
        var office = await officeResolver.GetLinkedOfficeAsync(cancellationToken).ConfigureAwait(false);
        var agencyCode = office?.Code?.Trim() ?? entity.AnnexECode;

        return new OrganizationProfileDto(
            entity.Id, entity.Name, entity.ShortName, entity.Address, entity.LogoUrl, agencyCode,
            entity.ApprovingOfficialId, entity.ApprovingOfficialName, entity.ApprovingOfficialDesignation,
            entity.AssistantRegionalManagerId, entity.AssistantRegionalManagerName, entity.AssistantRegionalManagerDesignation,
            entity.AccountantId, entity.AccountantName, entity.AccountantDesignation,
            entity.SupervisingAdminOfficerId, entity.SupervisingAdminOfficerName, entity.SupervisingAdminOfficerDesignation,
            entity.BudgetOfficerId, entity.BudgetOfficerName, entity.BudgetOfficerDesignation,
            entity.PropertyCustodianId, entity.PropertyCustodianName, entity.PropertyCustodianDesignation,
            office?.Id, office?.Name);
    }
}
