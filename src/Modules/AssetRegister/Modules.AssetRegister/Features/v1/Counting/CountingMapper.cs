using AMIS.Modules.AssetRegister.Contracts.v1.Counting;
using AMIS.Modules.AssetRegister.Domain.Counting;
using AMIS.Modules.AssetRegister.Features.v1.Accountability;

namespace AMIS.Modules.AssetRegister.Features.v1.Counting;

internal static class CountingMapper
{
    public static PhysicalCountEntryDto ToDto(PhysicalCountEntry e) =>
        new(e.Id, e.SessionId, e.AssetRegistryId,
            e.Snapshot is null ? null : AccountabilityMapper.ToDto(e.Snapshot),
            e.SnapshotArticle, e.SnapshotUnit, e.SnapshotUnitCost, e.Condition,
            e.ScannedOnUtc, e.PhotoPath, e.ScannedByEmployeeId, e.LocationId, e.Remarks,
            e.ProposedPropertyClass, e.ProposedCategoryCode, e.ProposedAcquisitionDate, e.ProposedUnitCost);

    public static PhysicalCountSessionDto ToDto(PhysicalCountSession s) =>
        new(s.Id, s.Code, s.Scope, s.Status, s.FundCluster, s.StartedOn, s.ClosedOn, s.AsAt, s.Remarks,
            s.ConductedBy.Select(AccountabilityMapper.ToDto).ToList(),
            s.ApprovedBy is null ? null : AccountabilityMapper.ToDto(s.ApprovedBy),
            s.WitnessedBy is null ? null : AccountabilityMapper.ToDto(s.WitnessedBy),
            s.Entries.Select(ToDto).ToList());
}

