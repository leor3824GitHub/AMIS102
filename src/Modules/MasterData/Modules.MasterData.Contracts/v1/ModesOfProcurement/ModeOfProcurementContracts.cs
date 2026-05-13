namespace AMIS.Modules.MasterData.Contracts.v1.ModesOfProcurement;

public sealed record ModeOfProcurementDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public sealed record CreateModeOfProcurementCommand(
    string Name,
    string? Description = null);

public sealed record UpdateModeOfProcurementCommand(
    Guid Id,
    string Name,
    string? Description = null,
    bool IsActive = true);

public sealed record GetModeOfProcurementQuery(Guid Id);

public sealed record DeleteModeOfProcurementCommand(Guid Id);

