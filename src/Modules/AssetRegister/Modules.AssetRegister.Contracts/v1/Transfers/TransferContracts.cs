using AMIS.Framework.Shared.Persistence;
using Mediator;

namespace AMIS.Modules.AssetRegister.Contracts.v1.Transfers;

/// <summary>Which side of the two-row inter-agency transfer handshake a row represents.</summary>
public enum TransferOfferDirection
{
    /// <summary>The sending agency's copy, raised when it posts the PPEIR/SMIR.</summary>
    Outbound = 0,
    /// <summary>The receiving agency's copy, projected in by the cross-tenant projector.</summary>
    Inbound = 1
}

public enum TransferOfferStatus
{
    /// <summary>Offered and awaiting the receiving agency's decision.</summary>
    Sent = 0,
    /// <summary>Receiver accepted and posted its own PPERR/SMRR.</summary>
    Accepted = 1,
    /// <summary>Receiver declined. The sender's assets stay TransferredOut — reversing that is a separate document.</summary>
    Rejected = 2,
    /// <summary>Sender withdrew the offer before the receiver answered.</summary>
    Cancelled = 3
}

// ── DTOs ───────────────────────────────────────────────────────────────────

public sealed record AssetTransferOfferLineDto(
    Guid Id,
    int ItemNo,
    string SourcePropertyNo,
    string Description,
    string? SerialNo,
    string? Brand,
    string? Model,
    decimal UnitCost,
    DateOnly OriginalAcquisitionDate,
    decimal AccumulatedDepreciation,
    DateOnly? DepreciationCurrentThrough,
    decimal NetBookValue,
    string? CatalogUacsCode);

public sealed record AssetTransferOfferDto(
    Guid Id,
    Guid CorrelationId,
    TransferOfferDirection Direction,
    string FromTenantId,
    string FromAgencyName,
    string ToTenantId,
    string ToAgencyName,
    string SourceIssuanceReportNo,
    IssuanceReportType IssuanceReportType,
    TransferOfferStatus Status,
    Guid? ReceivingReportId,
    string? ReceivingReportNo,
    string? RejectedReason,
    DateTimeOffset? RespondedUtc,
    DateTimeOffset CreatedOnUtc,
    decimal TotalUnitCost,
    decimal TotalNetBookValue,
    IReadOnlyCollection<AssetTransferOfferLineDto> Lines);

public sealed record AssetTransferOfferSummaryDto(
    Guid Id,
    Guid CorrelationId,
    TransferOfferDirection Direction,
    string FromAgencyName,
    string ToAgencyName,
    string SourceIssuanceReportNo,
    IssuanceReportType IssuanceReportType,
    TransferOfferStatus Status,
    string? ReceivingReportNo,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? RespondedUtc,
    int LineCount,
    decimal TotalNetBookValue);

/// <summary>An agency this tenant may offer property to. Id + display name only.</summary>
public sealed record TransferDestinationDto(string TenantId, string AgencyName);

// ── Queries ────────────────────────────────────────────────────────────────

/// <summary>
/// Lists the active agencies this tenant can transfer property to, excluding itself. Deliberately minimal —
/// identifier and display name only, never connection strings or subscription data — so it can be gated by
/// the ordinary transfer permission instead of the admin-only tenant-administration permission.
/// </summary>
public sealed record GetTransferDestinationsQuery : IQuery<IReadOnlyList<TransferDestinationDto>>;

/// <summary>
/// Resolves the agency a recipient employee belongs to, so the PPEIR form can derive the destination from
/// who the property is being issued to instead of asking for it a second time.
/// <para>
/// Returns null whenever there is no linked destination — a hand-typed recipient, an office no tenant
/// claims, or a colleague in the sender's own agency. Null is an ordinary answer, never an error.
/// </para>
/// </summary>
public sealed record ResolveTransferDestinationQuery(Guid EmployeeId) : IQuery<TransferDestinationDto?>;

/// <summary>
/// The receiving agency's "Incoming Transfers" inbox. Returns only this tenant's own rows — the ambient
/// tenant filter does the scoping, there is no cross-tenant read here.
/// </summary>
public sealed record SearchTransferOffersQuery(
    TransferOfferDirection? Direction = null,
    TransferOfferStatus? Status = null,
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResponse<AssetTransferOfferSummaryDto>>;

public sealed record GetTransferOfferQuery(Guid Id) : IQuery<AssetTransferOfferDto?>;

// ── Commands ───────────────────────────────────────────────────────────────

/// <summary>
/// Links an inbound offer to the PPERR/SMRR the receiving agency just posted on its own form series.
/// The receiving report is created by the ordinary CreateReceivingReport flow first; this only records
/// the link and flips the offer to Accepted. The projector carries the response back to the sender.
/// </summary>
public sealed record AcceptTransferOfferCommand(
    Guid Id,
    Guid ReceivingReportId) : ICommand<AssetTransferOfferDto>;

public sealed record RejectTransferOfferCommand(
    Guid Id,
    string Reason) : ICommand<AssetTransferOfferDto>;
