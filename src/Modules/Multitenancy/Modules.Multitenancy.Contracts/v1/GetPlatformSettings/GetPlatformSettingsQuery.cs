using AMIS.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.GetPlatformSettings;

public sealed record GetPlatformSettingsQuery : IQuery<PlatformSettingsDto>;
