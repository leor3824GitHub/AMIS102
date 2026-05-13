using AMIS.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.GetTenantTheme;

public sealed record GetTenantThemeQuery : IQuery<TenantThemeDto>;

