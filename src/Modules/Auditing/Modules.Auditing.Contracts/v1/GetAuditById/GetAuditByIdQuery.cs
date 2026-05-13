using AMIS.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Auditing.Contracts.v1.GetAuditById;

public sealed record GetAuditByIdQuery(Guid Id) : IQuery<AuditDetailDto>;


