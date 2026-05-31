using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;

public sealed record PrintVehicleInventoryQuery(
    string?   Status,
    DateTime? AsOfDate) : IQuery<byte[]>;
