using AMIS.Framework.Shared.Identity.Authorization;
using AMIS.Modules.AssetRegister.Contracts.Permissions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMIS.Modules.AssetRegister.Features.v1.Assets.GetNextPropertyNoSequence;

public static class GetNextPropertyNoSequenceEndpoint
{
    public static RouteHandlerBuilder Map(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/next-property-no-sequence", Handle)
            .WithModuleName<GetNextPropertyNoSequenceQuery>()
            .WithSummary("Preview the next property-number sequence for a year/office/class prefix without consuming it")
            .Produces<NextPropertyNoSequenceResponse>()
            .RequirePermission(AssetRegisterPermissions.Assets.View);

    private static async Task<IResult> Handle(
        int year,
        string officeCode,
        string classCode,
        IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetNextPropertyNoSequenceQuery(year, officeCode, classCode), ct);
        return TypedResults.Ok(result);
    }
}
