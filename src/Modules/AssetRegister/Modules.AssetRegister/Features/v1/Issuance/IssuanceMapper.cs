using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;
using AMIS.Modules.AssetRegister.Domain.Issuance;
using AMIS.Modules.AssetRegister.Features.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance;

internal static class IssuanceMapper
{
    public static PropertyIssuanceReportLineDto ToDto(PropertyIssuanceReportLine l) =>
        new(l.Id, l.ReportId, l.AssetRegistryId, l.ItemNo,
            AccountabilityMapper.ToDto(l.Snapshot),
            l.SnapshotUnitCost, l.SnapshotAmount,
            l.AccumulatedDepreciation, l.BookValue);

    public static PropertyIssuanceReportDto ToDto(PropertyIssuanceReport r) =>
        new(r.Id, r.ReportNo, r.ReportType, r.FundCluster, r.Date, r.Nature,
            AccountabilityMapper.ToDto(r.IssuedBy),
            AccountabilityMapper.ToDto(r.ApprovedBy),
            AccountabilityMapper.ToDto(r.IssuedTo),
            r.IssuedToOfficeAddress,
            r.Remarks,
            r.Lines.Select(ToDto).ToList());
}
