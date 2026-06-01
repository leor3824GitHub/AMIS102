using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintPhysicalCount;

public sealed record PrintPhysicalCountQuery(
    Guid?     WarehouseLocationId,
    DateTime? AsOfDate,
    DateTime? AssumedAccountabilityDate = null,
    string    PaperSize   = "a4",
    string    Orientation = "landscape",
    double    Margin      = 15d) : IQuery<byte[]>;
