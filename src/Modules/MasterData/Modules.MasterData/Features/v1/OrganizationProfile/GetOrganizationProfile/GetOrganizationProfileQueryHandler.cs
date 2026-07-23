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

        // Resolved on read, not only on save: when an administrator re-links the tenant to another office the
        // profile follows immediately, without anyone having to open and re-save it. Falls back to the stored
        // code so an unlinked tenant keeps whatever it was set up with.
        var office = await officeResolver.GetLinkedOfficeAsync(cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            // An administrator links the tenant to its office before anyone fills the profile in, so reporting
            // the link here is what stops a linked agency being told it "isn't linked to an office" until after
            // its first save. Callers keep treating this as unconfigured: they gate on a blank name, not on
            // null. Still null when there is no office either — nothing to report, and report headers stay
            // hidden for a tenant that has set up neither.
            return office is null
                ? null
                : new OrganizationProfileDto(
                    Guid.Empty, string.Empty, null, null, null, office.Code?.Trim(),
                    OfficeId: office.Id, OfficeName: office.Name);
        }

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
