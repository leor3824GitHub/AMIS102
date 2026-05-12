using FSH.Modules.AssetRegister.Contracts.v1.Issuance;
using FSH.Modules.AssetRegister.Domain.Issuance;
using FSH.Modules.AssetRegister.Features.v1.Accountability;

namespace FSH.Modules.AssetRegister.Features.v1.Issuance;

internal static class IssuanceMapper
{
    public static PropertyIssuanceReportLineDto ToDto(PropertyIssuanceReportLine l) =>
        new(l.Id, l.ReportId, l.AccountabilityId, l.AccountabilityLineId, l.AssetRegistryId,
            AccountabilityMapper.ToDto(l.Snapshot), l.SnapshotResponsibilityCenterCode,
            l.SnapshotQuantityIssued, l.SnapshotUnitCost, l.SnapshotAmount);

    public static PropertyIssuanceReportDto ToDto(PropertyIssuanceReport r) =>
        new(r.Id, r.ReportNo, r.ReportType, r.FundCluster, r.PeriodFromDate, r.PeriodToDate, r.Status,
            AccountabilityMapper.ToDto(r.PreparedBy),
            r.CertifiedBy is null ? null : AccountabilityMapper.ToDto(r.CertifiedBy),
            r.PostedBy is null ? null : AccountabilityMapper.ToDto(r.PostedBy),
            r.PostedOn,
            r.Lines.Select(ToDto).ToList());
}
