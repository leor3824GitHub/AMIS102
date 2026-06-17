using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.Finance.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Finance.PrintDisbursementVoucher;

internal static class PrintDisbursementVoucherEndpoint
{
    internal static void Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/disbursement-vouchers/{id:guid}/pdf",
            async (Guid id, IMediator mediator, CancellationToken ct,
                   string? pageWidth, string? orientation, double? marginMm) =>
            {
                var paperSize = (pageWidth ?? "a4").ToLowerInvariant();
                var orient = (orientation ?? "portrait").ToLowerInvariant() == "landscape" ? "landscape" : "portrait";
                var margin = marginMm is > 0 ? marginMm.Value : 14d;
                var bytes = await mediator.Send(new PrintDisbursementVoucherQuery(id, paperSize, orient, margin), ct);
                return TypedResults.File(bytes, "application/pdf", "DisbursementVoucher.pdf");
            })
            .WithName("QuestPdfReporting_PrintDisbursementVoucher")
            .WithSummary("Generate the Disbursement Voucher (DV) PDF document")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .RequirePermission(FinancePermissions.DisbursementVouchers.View);
}
