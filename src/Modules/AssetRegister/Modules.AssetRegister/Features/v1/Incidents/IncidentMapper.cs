using FSH.Modules.AssetRegister.Contracts.v1.Incidents;
using FSH.Modules.AssetRegister.Domain.Incidents;
using FSH.Modules.AssetRegister.Features.v1.Accountability;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents;

internal static class IncidentMapper
{
    public static PropertyIncidentItemDto ToDto(PropertyIncidentItem i) =>
        new(i.Id, i.ReportId, i.AssetRegistryId, AccountabilityMapper.ToDto(i.Snapshot),
            i.SnapshotAcquisitionCost, i.SnapshotCurrentReplacementCost,
            i.AccountabilityLineId, i.ItemResolution, i.ResolvedOn);

    public static PropertyIncidentReportDto ToDto(PropertyIncidentReport r) =>
        new(r.Id, r.IncidentNo, r.IncidentType, r.IncidentDate, r.FundCluster, r.DepartmentOffice, r.Circumstances,
            AccountabilityMapper.ToDto(r.AccountableOfficer), r.AccountableOfficerDesignation,
            r.AccountableOfficerGovIdType, r.AccountableOfficerGovIdNo, r.AccountableOfficerGovIdIssuedOn,
            r.NotedBy is null ? null : AccountabilityMapper.ToDto(r.NotedBy),
            r.PoliceNotified, r.PoliceStation, r.PoliceNotifiedOn, r.PoliceBlotterRef,
            r.NotarizedOn, r.NotaryDocNo, r.NotaryPageNo, r.NotaryBookNo, r.NotarySeriesOf,
            r.Status, r.ReliefRequestedOn, r.ReliefGrantedOn, r.ReliefGrantedRef,
            r.AmountSettled, r.SettledOn, r.RecoveredOn,
            r.Items.Select(ToDto).ToList());
}
