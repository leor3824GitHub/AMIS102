namespace AMIS.Modules.MasterData.Contracts.v1.FundClusters;

public sealed record FundClusterDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);

public sealed record CreateFundClusterCommand(
    string Code,
    string Name,
    string? Description = null);

public sealed record UpdateFundClusterCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    bool IsActive = true);
