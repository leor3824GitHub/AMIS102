using AMIS.Modules.AssetRegister.Contracts.v1;

namespace AMIS.Modules.AssetRegister.Domain.Services;

public interface IAccountabilityNumberGenerator
{
    /// <summary>SPLV-YYYY-MM-NNNN (low-valued) or SPHV-YYYY-MM-NNNN (high-valued).</summary>
    Task<string> NextIcsAsync(AssetCategory category, DateOnly issueDate, CancellationToken ct);

    /// <summary>PAR-YYYY-MM-NNNN.</summary>
    Task<string> NextParAsync(DateOnly issueDate, CancellationToken ct);
}

public interface IInventoryTransferNumberGenerator
{
    /// <summary>ITR-YYYY-MM-NNNN.</summary>
    Task<string> NextAsync(DateOnly date, CancellationToken ct);
}

public interface IIncidentNumberGenerator
{
    /// <summary>RLSDDSP-YYYY-MM-NNNN.</summary>
    Task<string> NextAsync(DateOnly incidentDate, CancellationToken ct);
}

public interface IIssuanceReportNumberGenerator
{
    /// <summary>RSPI-YYYY-MM-NNNN (SMIR) or PPEIR-YYYY-MM-NNNN.</summary>
    Task<string> NextAsync(IssuanceReportType type, DateOnly periodStart, CancellationToken ct);
}

public interface IUnserviceableReportNumberGenerator
{
    /// <summary>IIRUSP-YYYY-MM-NNNN or IIRUP-YYYY-MM-NNNN.</summary>
    Task<string> NextAsync(UnserviceableReportType type, DateOnly asAt, CancellationToken ct);
}

public interface IReceivingReportNumberGenerator
{
    /// <summary>PPERR-YYYY-MM-NNNN or SMRR-YYYY-MM-NNNN.</summary>
    Task<string> NextAsync(ReceivingDocumentKind kind, DateOnly date, CancellationToken ct);
}

/// <summary>
/// Computes current replacement cost per COA 2022-004 §4.19 for incident-report
/// snapshotting.
/// </summary>
public interface ICurrentReplacementCostCalculator
{
    Task<decimal> ComputeAsync(Guid assetRegistryId, DateOnly asOf, CancellationToken ct);
}

