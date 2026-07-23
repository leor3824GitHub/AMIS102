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

        // Name and address are stored, but they were taken from whichever office the tenant was linked to at
        // the time — so after a re-link they still describe the previous agency while the code beside them
        // already shows the new one. Report the new office's details instead, on the same read-resolution the
        // code uses, so the whole profile follows the link together rather than in pieces.
        //
        // Deliberate edits survive, because a save re-stamps SourceOfficeId to the office they were made
        // against; only a *different* office means the stored text was left behind. ShortName and LogoUrl are
        // agency-wide branding rather than office-specific, so a re-link leaves them alone.
        var isFromAnotherOffice = office is not null && entity.SourceOfficeId != office.Id;

        var name = isFromAnotherOffice ? office!.Name : entity.Name;
        var address = isFromAnotherOffice ? office!.Address : entity.Address;

        return new OrganizationProfileDto(
            entity.Id, name, entity.ShortName, address, entity.LogoUrl, agencyCode,
            entity.ApprovingOfficialId, entity.ApprovingOfficialName, entity.ApprovingOfficialDesignation,
            entity.AssistantRegionalManagerId, entity.AssistantRegionalManagerName, entity.AssistantRegionalManagerDesignation,
            entity.AccountantId, entity.AccountantName, entity.AccountantDesignation,
            entity.SupervisingAdminOfficerId, entity.SupervisingAdminOfficerName, entity.SupervisingAdminOfficerDesignation,
            entity.BudgetOfficerId, entity.BudgetOfficerName, entity.BudgetOfficerDesignation,
            entity.PropertyCustodianId, entity.PropertyCustodianName, entity.PropertyCustodianDesignation,
            office?.Id, office?.Name);
    }
}
