using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.MasterData.Contracts.v1.References;

public sealed record EmployeeReferenceDto(
    Guid Id,
    string EmployeeNumber,
    string? IdentityUserId,
    string FirstName,
    string LastName,
    string? WorkEmail,
    Guid OfficeId,
    string OfficeCode,
    string OfficeName,
    Guid DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    Guid PositionId,
    string PositionCode,
    string PositionName,
    Guid? DefaultUnitOfMeasureId,
    string? DefaultUnitOfMeasureCode,
    string? DefaultUnitOfMeasureName,
    bool IsActive,
    string? OwnerOfficeCode = null);

public sealed record OfficeReferenceDto(Guid Id, string Code, string Name, string? Description, string? Address, string? LocationCode, string? RegProvCode, bool IsActive, string? OfficeCode = null);

public sealed record CreateOfficeCommand(string Code, string Name, string? Description, string? Address = null, string? LocationCode = null, string? RegProvCode = null, bool IsActive = true, string? OfficeCode = null) : ICommand<OfficeReferenceDto>;

public sealed record UpdateOfficeCommand(Guid Id, string Code, string Name, string? Description, string? Address, string? LocationCode, string? RegProvCode, bool IsActive) : ICommand<OfficeReferenceDto>;

public sealed record DeleteOfficeCommand(Guid Id) : ICommand<Unit>;

public sealed record DepartmentReferenceDto(Guid Id, string Code, string Name, string? Description, bool IsActive, string? OfficeCode = null);

public sealed record CreateDepartmentCommand(string Code, string Name, string? Description, string? FundCluster = null, bool IsActive = true, string? OfficeCode = null) : ICommand<DepartmentReferenceDto>;

public sealed record UpdateDepartmentCommand(Guid Id, string Code, string Name, string? Description, string? FundCluster, bool IsActive) : ICommand<DepartmentReferenceDto>;

public sealed record DeleteDepartmentCommand(Guid Id) : ICommand<Unit>;

public sealed record PositionReferenceDto(Guid Id, string Code, string Name, string? Description, bool IsActive, string? OfficeCode = null);

public sealed record CreatePositionCommand(string Code, string Name, string? Description, bool IsActive = true, string? OfficeCode = null) : ICommand<PositionReferenceDto>;

public sealed record UpdatePositionCommand(Guid Id, string Code, string Name, string? Description, bool IsActive) : ICommand<PositionReferenceDto>;

public sealed record DeletePositionCommand(Guid Id) : ICommand<Unit>;

public sealed record UnitOfMeasureReferenceDto(Guid Id, string Code, string Name, string? Description, bool IsActive, string? OfficeCode = null);

public sealed record CreateUnitOfMeasureCommand(string Code, string Name, string? Description, bool IsActive = true, string? OfficeCode = null) : ICommand<UnitOfMeasureReferenceDto>;

public sealed record UpdateUnitOfMeasureCommand(Guid Id, string Code, string Name, string? Description, bool IsActive) : ICommand<UnitOfMeasureReferenceDto>;

public sealed record DeleteUnitOfMeasureCommand(Guid Id) : ICommand<Unit>;

public sealed record CreateEmployeeCommand(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    Guid OfficeId,
    Guid DepartmentId,
    Guid PositionId,
    string? IdentityUserId = null,
    string? WorkEmail = null,
    Guid? DefaultUnitOfMeasureId = null,
    bool IsActive = true,
    string? OfficeCode = null) : ICommand<EmployeeReferenceDto>;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    Guid OfficeId,
    Guid DepartmentId,
    Guid PositionId,
    string? IdentityUserId,
    string? WorkEmail,
    Guid? DefaultUnitOfMeasureId,
    bool IsActive) : ICommand<EmployeeReferenceDto>;

public sealed record DeleteEmployeeCommand(Guid Id) : ICommand<Unit>;

public sealed record GetEmployeeReferenceByIdQuery(Guid Id) : IQuery<EmployeeReferenceDto?>;

public sealed record GetEmployeeReferencesByIdsQuery(IReadOnlyCollection<Guid> Ids)
    : IQuery<IReadOnlyDictionary<Guid, EmployeeReferenceDto>>;

public sealed record GetEmployeeReferenceByIdentityUserIdQuery(string IdentityUserId) : IQuery<EmployeeReferenceDto?>;

public sealed class SearchEmployeeReferencesQuery : IPagedQuery, IQuery<PagedResponse<EmployeeReferenceDto>>
{
    public string? Keyword { get; set; }
    public string? IdentityUserId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
    public bool? IsActive { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed class ListOfficeReferencesQuery : IPagedQuery, IQuery<PagedResponse<OfficeReferenceDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetOfficeReferenceByIdQuery(Guid Id) : IQuery<OfficeReferenceDto?>;

public sealed class ListDepartmentReferencesQuery : IPagedQuery, IQuery<PagedResponse<DepartmentReferenceDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetDepartmentReferenceByIdQuery(Guid Id) : IQuery<DepartmentReferenceDto?>;

public sealed class ListPositionReferencesQuery : IPagedQuery, IQuery<PagedResponse<PositionReferenceDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetPositionReferenceByIdQuery(Guid Id) : IQuery<PositionReferenceDto?>;

public sealed class ListUnitOfMeasureReferencesQuery : IPagedQuery, IQuery<PagedResponse<UnitOfMeasureReferenceDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

public sealed record GetUnitOfMeasureReferenceByIdQuery(Guid Id) : IQuery<UnitOfMeasureReferenceDto?>;
