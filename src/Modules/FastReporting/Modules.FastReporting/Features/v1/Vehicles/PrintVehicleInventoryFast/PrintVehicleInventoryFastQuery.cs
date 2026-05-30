using AMIS.Modules.FastReporting.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.FastReporting.Features.v1.Vehicles.PrintVehicleInventoryFast;

public sealed record PrintVehicleInventoryFastQuery(
    string? Status = null,
    DateTime? AsOfDate = null,
    string PaperSize = "a4",
    string Orientation = "landscape") : IQuery<ReportFileDto>;
