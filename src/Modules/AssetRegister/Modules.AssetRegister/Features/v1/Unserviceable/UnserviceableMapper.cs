using FSH.Modules.AssetRegister.Contracts.v1.Unserviceable;
using FSH.Modules.AssetRegister.Domain.Unserviceable;
using FSH.Modules.AssetRegister.Features.v1.Accountability;

namespace FSH.Modules.AssetRegister.Features.v1.Unserviceable;

internal static class UnserviceableMapper
{
    public static UnserviceablePropertyItemDto ToDto(UnserviceablePropertyItem i) =>
        new(i.Id, i.ReportId, i.AssetRegistryId, AccountabilityMapper.ToDto(i.Snapshot),
            i.SnapshotDateAcquired, i.SnapshotAcquisitionCost, i.SnapshotAccumulatedDepreciation,
            i.SnapshotAccumulatedImpairmentLosses, i.SnapshotCarryingAmount, i.Remarks,
            i.DisposalMethod, i.DisposalOtherSpecify, i.AppraisedValue, i.DisposalRecordedOn,
            i.SaleORNo, i.SaleAmount);

    public static UnserviceablePropertyReportDto ToDto(UnserviceablePropertyReport r) =>
        new(r.Id, r.ReportNo, r.ReportType, r.AsAt, r.FundCluster, r.Station, r.Status,
            AccountabilityMapper.ToDto(r.AccountableOfficer),
            r.ApprovedBy is null ? null : AccountabilityMapper.ToDto(r.ApprovedBy),
            r.InspectedBy is null ? null : AccountabilityMapper.ToDto(r.InspectedBy),
            r.InspectedOn,
            r.WitnessedBy is null ? null : AccountabilityMapper.ToDto(r.WitnessedBy),
            r.WitnessedOn,
            r.Items.Select(ToDto).ToList());
}
