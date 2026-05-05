using Mediator;

namespace FSH.Modules.MasterData.Features.v1.Employees.GetMyEmployee;

public sealed record GetMyEmployeeQuery : IQuery<MyEmployeeDto>;

public sealed record MyEmployeeDto(
    Guid EmployeeId,
    string FullName,
    string? Department,
    string? Position);
