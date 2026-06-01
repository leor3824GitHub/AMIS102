using AMIS.Modules.Multitenancy.Contracts;
using AMIS.Modules.Multitenancy.Contracts.Dtos;
using AMIS.Modules.Multitenancy.Contracts.v1.GetPlatformSettings;
using Mediator;

namespace AMIS.Modules.Multitenancy.Features.v1.GetPlatformSettings;

public sealed class GetPlatformSettingsQueryHandler(IPlatformSettingsService settingsService)
    : IQueryHandler<GetPlatformSettingsQuery, PlatformSettingsDto>
{
    public async ValueTask<PlatformSettingsDto> Handle(GetPlatformSettingsQuery query, CancellationToken cancellationToken)
    {
        return await settingsService.GetAsync(cancellationToken);
    }
}
