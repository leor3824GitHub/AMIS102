using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.PhysicalCount.SubmitPhysicalCountSession;

public sealed record SubmitPhysicalCountSessionCommand(Guid SessionId) : ICommand<SubmitPhysicalCountSessionResult>;

public sealed record SubmitPhysicalCountSessionResult(
    Guid SessionId,
    string SessionNo,
    int TotalEntries,
    int Found,
    int NotFound,
    int FoundAtStation,
    DateTimeOffset SubmittedOnUtc);

