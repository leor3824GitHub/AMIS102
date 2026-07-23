using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Data.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.OrganizationProfile.UpsertOrganizationProfile;

public sealed class UpsertOrganizationProfileCommandHandler(
    MasterDataDbContext db,
    ICurrentUser currentUser,
    AgencyOfficeResolver officeResolver)
    : ICommandHandler<UpsertOrganizationProfileCommand, OrganizationProfileDto>
{
    public async ValueTask<OrganizationProfileDto> Handle(
        UpsertOrganizationProfileCommand command, CancellationToken cancellationToken)
    {
        var existing = await db.OrganizationProfiles
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        // Never taken from the client: the office a tenant represents is set by an administrator on the
        // Tenants page and is what inter-agency transfers are routed by. The column is still written because
        // ownership stamping reads it directly; an unlinked tenant keeps the code it already had.
        var office = await officeResolver.GetLinkedOfficeAsync(cancellationToken).ConfigureAwait(false);
        var agencyCode = office?.Code?.Trim() ?? existing?.AnnexECode;

        // What is being saved is the agency name and address as they were shown for the office linked right
        // now, so this is where they stop being the previous office's and become this one's. Recording it is
        // what lets a later read tell a deliberate edit from details left behind by a re-link.
        var sourceOfficeId = office?.Id;

        if (existing is null)
        {
            var tenantId = currentUser.GetTenant() ?? string.Empty;
            existing = Domain.OrganizationProfile.Create(
                tenantId, command.Name, command.ShortName, command.Address, command.LogoUrl, agencyCode,
                sourceOfficeId,
                command.ApprovingOfficialId, command.ApprovingOfficialName, command.ApprovingOfficialDesignation,
                command.AssistantRegionalManagerId, command.AssistantRegionalManagerName, command.AssistantRegionalManagerDesignation,
                command.AccountantId, command.AccountantName, command.AccountantDesignation,
                command.SupervisingAdminOfficerId, command.SupervisingAdminOfficerName, command.SupervisingAdminOfficerDesignation,
                command.BudgetOfficerId, command.BudgetOfficerName, command.BudgetOfficerDesignation,
                command.PropertyCustodianId, command.PropertyCustodianName, command.PropertyCustodianDesignation);
            existing.CreatedBy = currentUser.GetUserId().ToString();
            db.OrganizationProfiles.Add(existing);
        }
        else
        {
            existing.Update(command.Name, command.ShortName, command.Address, command.LogoUrl, agencyCode,
                sourceOfficeId,
                command.ApprovingOfficialId, command.ApprovingOfficialName, command.ApprovingOfficialDesignation,
                command.AssistantRegionalManagerId, command.AssistantRegionalManagerName, command.AssistantRegionalManagerDesignation,
                command.AccountantId, command.AccountantName, command.AccountantDesignation,
                command.SupervisingAdminOfficerId, command.SupervisingAdminOfficerName, command.SupervisingAdminOfficerDesignation,
                command.BudgetOfficerId, command.BudgetOfficerName, command.BudgetOfficerDesignation,
                command.PropertyCustodianId, command.PropertyCustodianName, command.PropertyCustodianDesignation);
            existing.LastModifiedBy = currentUser.GetUserId().ToString();
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new OrganizationProfileDto(
            existing.Id, existing.Name, existing.ShortName, existing.Address, existing.LogoUrl, existing.AnnexECode,
            existing.ApprovingOfficialId, existing.ApprovingOfficialName, existing.ApprovingOfficialDesignation,
            existing.AssistantRegionalManagerId, existing.AssistantRegionalManagerName, existing.AssistantRegionalManagerDesignation,
            existing.AccountantId, existing.AccountantName, existing.AccountantDesignation,
            existing.SupervisingAdminOfficerId, existing.SupervisingAdminOfficerName, existing.SupervisingAdminOfficerDesignation,
            existing.BudgetOfficerId, existing.BudgetOfficerName, existing.BudgetOfficerDesignation,
            existing.PropertyCustodianId, existing.PropertyCustodianName, existing.PropertyCustodianDesignation,
            office?.Id, office?.Name);
    }
}
