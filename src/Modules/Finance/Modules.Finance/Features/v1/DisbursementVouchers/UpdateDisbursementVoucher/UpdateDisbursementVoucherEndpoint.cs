using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using AMIS.Modules.Finance.Contracts.v1.DisbursementVouchers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.Finance.Features.v1.DisbursementVouchers.UpdateDisbursementVoucher;

public static class UpdateDisbursementVoucherEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPut("/{id:guid}", async (Guid id, UpdateDisbursementVoucherCommand command, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(command with { Id = id }, ct);
            return TypedResults.NoContent();
        })
        .WithName(nameof(UpdateDisbursementVoucherCommand))
        .WithSummary("Edit a Draft disbursement voucher")
        .RequirePermission(FinancePermissions.DisbursementVouchers.Create);
}
