using Mediator;

namespace FSH.Modules.MasterData.Features.v1.ModesOfProcurement.GetModesOfProcurement;

public sealed record GetModesOfProcurementQuery(
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponseOfModeOfProcurementDto>;

public sealed record PagedResponseOfModeOfProcurementDto(
    ICollection<ModeOfProcurementDto>? Items,
    int PageNumber,
    int PageSize,
    int TotalCount);

public sealed record ModeOfProcurementDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
