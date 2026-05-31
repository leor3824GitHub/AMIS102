using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;

public sealed record PrintVehicleInventoryQuery(
    string?   Status,
    DateTime? AsOfDate,
    string    PaperSize   = "a4",
    string    Orientation = "landscape") : IQuery<byte[]>;
