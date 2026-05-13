using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.Vehicle.Contracts.v1.Repairs;

public record RepairRecordDto(
    Guid Id,
    Guid VehicleId,
    DateTimeOffset RepairDate,
    string Description,
    decimal Cost,
    string? VendorName,
    string? VendorContact,
    string? PartsUsed,
    string Status,
    DateTimeOffset? CompletedDate,
    string? Notes,
    DateTimeOffset CreatedOnUtc,
    string? CreatedBy,
    DateTimeOffset? LastModifiedOnUtc,
    string? LastModifiedBy);

public record CreateRepairRecordCommand(
    Guid VehicleId,
    DateTimeOffset RepairDate,
    string Description,
    decimal Cost,
    string? VendorName = null,
    string? VendorContact = null,
    string? PartsUsed = null,
    string? Notes = null) : ICommand<RepairRecordDto>;

public record UpdateRepairRecordCommand(
    Guid Id,
    DateTimeOffset RepairDate,
    string Description,
    decimal Cost,
    string? VendorName,
    string? VendorContact,
    string? PartsUsed,
    string? Notes) : ICommand<RepairRecordDto>;

public record StartRepairCommand(Guid Id) : ICommand<Unit>;

public record CompleteRepairCommand(
    Guid Id,
    DateTimeOffset CompletedDate) : ICommand<Unit>;

public record CancelRepairCommand(Guid Id) : ICommand<Unit>;

public record DeleteRepairRecordCommand(Guid Id) : ICommand<Unit>;

public record GetRepairRecordQuery(Guid Id) : IQuery<RepairRecordDto?>;

public sealed class SearchRepairRecordsQuery : IPagedQuery, IQuery<PagedResponse<RepairRecordDto>>
{
    public Guid? VehicleId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public string? Keyword { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public string? Sort { get; set; }
}

