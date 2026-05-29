using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Data;
using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;
using AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.CreateCanvassRequest;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass.AwardCanvass;

public sealed class AwardCanvassCommandHandler(
    ProcurementDbContext dbContext,
    ICurrentUser currentUser,
    IMediator mediator) : ICommandHandler<AwardCanvassCommand, CanvassRequestDto>
{
    public async ValueTask<CanvassRequestDto> Handle(AwardCanvassCommand command, CancellationToken cancellationToken)
    {
        var canvass = await dbContext.CanvassRequests
            .Include(x => x.Quotations)
            .FirstOrDefaultAsync(x => x.Id == command.CanvassRequestId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Canvass request '{command.CanvassRequestId}' not found.");

        var awardedQuotation = canvass.Quotations.FirstOrDefault(q => q.Id == command.AwardedQuotationId)
            ?? throw new AMIS.Framework.Core.Exceptions.NotFoundException($"Quotation '{command.AwardedQuotationId}' not found in canvass request.");

        // Clear previous award flags
        foreach (var q in canvass.Quotations)
        {
            q.ClearAwarded();
        }

        // Freeze the ROPC committee that signed at award time so the Abstract of Canvass reprints
        // faithfully — even if the configured ReportSignatories table or org officers change later.
        var committee = await ResolveCommitteeAsync(cancellationToken).ConfigureAwait(false);

        awardedQuotation.MarkAwarded();
        canvass.Award(awardedQuotation.SupplierId, committee);
        canvass.LastModifiedBy = currentUser.GetUserId().ToString();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var pr = await dbContext.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == canvass.PurchaseRequestId, cancellationToken)
            .ConfigureAwait(false);

        return CreateCanvassRequestCommandHandler.MapToDto(canvass, pr?.PrNumber ?? string.Empty);
    }

    // Resolve the 6 Abstract-of-Canvass committee slots from the configured ReportSignatories table,
    // with org-profile officer fallbacks. Mirrors the slot mapping in PrintAbstractOfCanvassFastQueryHandler;
    // the result is frozen onto the canvass so the report can read it directly thereafter.
    private async ValueTask<IReadOnlyList<CanvassAwardSignatory>> ResolveCommitteeAsync(CancellationToken ct)
    {
        var signatories = await mediator.Send(new GetReportSignatoriesQuery("AbstractOfCanvass"), ct).ConfigureAwait(false);
        var org = await mediator.Send(new GetOrganizationProfileQuery(), ct).ConfigureAwait(false);

        string Name(int order, string? orgFallback = null) =>
            signatories.FirstOrDefault(s => s.SortOrder == order && s.IsActive)?.Name ?? orgFallback ?? string.Empty;
        string Role(int order, string? fallback = null) =>
            signatories.FirstOrDefault(s => s.SortOrder == order && s.IsActive)?.Label ?? fallback ?? string.Empty;

        return
        [
            CanvassAwardSignatory.Create(1, Name(1), Role(1, "TWG Goods/Services-Chairperson")),
            CanvassAwardSignatory.Create(2, Name(2, org?.AccountantName), Role(2, org?.AccountantDesignation ?? "Accountant IV")),
            CanvassAwardSignatory.Create(3, Name(3), Role(3, "ROPC Member")),
            CanvassAwardSignatory.Create(4, Name(4), Role(4, "ROPC Member")),
            CanvassAwardSignatory.Create(5, Name(5, org?.SupervisingAdminOfficerName),
                Role(5, org?.SupervisingAdminOfficerDesignation ?? "Supervising Administrative Officer")),
            CanvassAwardSignatory.Create(6, Name(6, org?.AssistantRegionalManagerName ?? org?.ApprovingOfficialName),
                Role(6, org?.AssistantRegionalManagerDesignation ?? org?.ApprovingOfficialDesignation ?? "Assistant Regional Manager II")),
        ];
    }
}

