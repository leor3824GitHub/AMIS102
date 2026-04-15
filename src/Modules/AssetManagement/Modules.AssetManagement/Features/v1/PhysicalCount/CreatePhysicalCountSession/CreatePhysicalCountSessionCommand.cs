using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.PhysicalCount.CreatePhysicalCountSession;

public sealed record CreatePhysicalCountSessionCommand(
    string SessionNo,
    DateOnly CountDate,
    string StationOffice,
    PhysicalCountScope Scope,
    Guid PreparedByEmployeeId,
    Guid CertifiedByEmployeeId,
    Guid ApprovedByEmployeeId) : ICommand<CreatePhysicalCountSessionResult>;

public sealed record CreatePhysicalCountSessionResult(
    Guid SessionId,
    string SessionNo,
    int EntriesCreated);
