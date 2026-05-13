using AMIS.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Multitenancy.Contracts.v1.GetTenantStatus;

public sealed record GetTenantStatusQuery(string TenantId) : IQuery<TenantStatusDto>;


