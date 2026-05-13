using AMIS.Modules.AssetRegister.Contracts.v1.Accountability;
using AMIS.Modules.AssetRegister.Contracts.v1.Receiving;
using AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;
using AMIS.Modules.AssetRegister.Domain.Receiving;

namespace AMIS.Modules.AssetRegister.Features.v1.Receiving;

internal static class ReceivingMapper
{
    public static EmployeeRefDto ToDto(EmployeeRef e) =>
        new(e.EmployeeId, e.PrintedName, e.Designation);

    public static EmployeeRefDto? ToDtoOrNull(EmployeeRef? e) =>
        e is null ? null : new EmployeeRefDto(e.EmployeeId, e.PrintedName, e.Designation);

    public static ReceivingReportItemDto ToDto(ReceivingReportItem i) =>
        new(i.Id, i.ReportId, i.CatalogItemId, i.Reference, i.Description,
            i.AcquisitionDate, i.Quantity, i.UnitCost, i.Amount,
            i.SerialNo, i.Brand, i.Model);

    public static ReceivingReportDto ToDto(ReceivingReport r) =>
        new(r.Id, r.DocumentKind, r.ReportNo, r.Date, r.ReceivedFrom, r.Address,
            r.ReceiptType, r.OtherReceiptType, r.FundCluster,
            ToDto(r.ReceivedBy), ToDtoOrNull(r.NotedBy), r.DateReceived,
            r.Items.Select(ToDto).ToList());
}

