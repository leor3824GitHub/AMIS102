using Playground.Maui.Data.Models;

namespace Playground.Maui.Services;

// DTOs for API responses
public sealed record TokenIssueRequest(string Email, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken);
public sealed record UserProfileDto(string Id, string Email, string? FirstName, string? LastName, string? ImageUrl);
public sealed record MyEmployeeDto(Guid EmployeeId, string FullName, string? Department, string? Position);

public sealed record ICSSummaryDto(
    Guid Id,
    string ICSNo,
    string Date,
    string Status,
    string? ExpiresOn,
    int ItemCount);

public sealed record PARSummaryDto(
    Guid Id,
    string PARNo,
    string Date,
    string PARType,
    int ItemCount);

public sealed record ICSDetailDto(
    Guid Id,
    string ICSNo,
    string Date,
    string Status,
    string? ExpiresOn,
    List<ICSItemDto> Items);

public sealed record ICSItemDto(
    Guid Id,
    string PropertyNo,
    string? Description,
    decimal UnitCost,
    int? EstimatedUsefulLifeYears);

public sealed record PARDetailDto(
    Guid Id,
    string PARNo,
    string Date,
    string PARType,
    List<PARItemDto> Items);

public sealed record PARItemDto(
    Guid Id,
    string PropertyNo,
    string ItemDescription,
    decimal UnitCost,
    int Quantity,
    int EstimatedUsefulLifeYears,
    string DateAcquired);

public sealed record TangibleInventoryItemDetailDto(
    Guid Id,
    string PropertyNo,
    string ItemName,
    string? Description,
    decimal UnitCost,
    string AssetType,
    bool IsIssued,
    string? LinkedDocumentType,
    string? LinkedDocumentNo,
    Guid? LinkedDocumentId);

public interface IApiClient
{
    Task<TokenResponse> IssueTokenAsync(string email, string password, CancellationToken ct = default);
    Task<UserProfileDto> GetMyProfileAsync(CancellationToken ct = default);
    Task<MyEmployeeDto> GetMyEmployeeAsync(CancellationToken ct = default);
    Task<List<ICSSummaryDto>> GetMyICSListAsync(Guid employeeId, CancellationToken ct = default);
    Task<ICSDetailDto> GetICSByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PARSummaryDto>> GetMyPARListAsync(Guid employeeId, CancellationToken ct = default);
    Task<PARDetailDto> GetPARByIdAsync(Guid id, CancellationToken ct = default);
    Task<TangibleInventoryItemDetailDto> GetItemByPropertyNoAsync(string propertyNo, CancellationToken ct = default);
}
