using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

public sealed record PrintPhysicalCountQuery(
    Guid?     WarehouseLocationId,
    DateTime? AsOfDate) : IQuery<byte[]>;
