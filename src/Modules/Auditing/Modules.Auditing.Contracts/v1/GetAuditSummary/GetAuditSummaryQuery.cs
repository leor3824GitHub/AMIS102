using AMIS.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace AMIS.Modules.Auditing.Contracts.v1.GetAuditSummary;

public sealed class GetAuditSummaryQuery : IQuery<AuditSummaryAggregateDto>
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? TenantId { get; init; }
}


