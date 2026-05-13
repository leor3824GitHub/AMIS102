using AMIS.Modules.ProcurementPlanning.Contracts.v1.Ppmps;
using AMIS.Modules.ProcurementPlanning.Domain.Ppmps;

namespace AMIS.Modules.ProcurementPlanning.Domain.AnnualProcurementPlans;

public sealed class AppSourcePpmp
{
    public Guid Id { get; private set; }
    public Guid AppId { get; private set; }
    public Guid PpmpId { get; private set; }
    public string PpmpNumber { get; private set; } = default!;
    public string OfficeCode { get; private set; } = default!;
    public string EndUserUnit { get; private set; } = default!;
    public PpmpPhase Phase { get; private set; }
    public int VersionNumber { get; private set; }
    public DateTimeOffset IncludedOnUtc { get; private set; }
    public Guid IncludedById { get; private set; }

    private AppSourcePpmp() { }

    internal static AppSourcePpmp FromPpmp(Guid appId, Ppmp ppmp, Guid includedById, DateTimeOffset includedOnUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            PpmpId = ppmp.Id,
            PpmpNumber = ppmp.PpmpNumber,
            OfficeCode = ppmp.OfficeCode,
            EndUserUnit = ppmp.EndUserUnit,
            Phase = ppmp.Phase,
            VersionNumber = ppmp.VersionNumber,
            IncludedOnUtc = includedOnUtc,
            IncludedById = includedById
        };

    internal static AppSourcePpmp Clone(Guid newAppId, AppSourcePpmp source) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = newAppId,
            PpmpId = source.PpmpId,
            PpmpNumber = source.PpmpNumber,
            OfficeCode = source.OfficeCode,
            EndUserUnit = source.EndUserUnit,
            Phase = source.Phase,
            VersionNumber = source.VersionNumber,
            IncludedOnUtc = source.IncludedOnUtc,
            IncludedById = source.IncludedById
        };
}

public sealed class AppLineItem
{
    public Guid Id { get; private set; }
    public Guid AppId { get; private set; }
    public Guid SourcePpmpId { get; private set; }
    public Guid SourcePpmpItemId { get; private set; }
    public string SourcePpmpNumber { get; private set; } = default!;
    public string OfficeCode { get; private set; } = default!;
    public string EndUserUnit { get; private set; } = default!;
    public int ItemNo { get; private set; }
    public string GeneralDescription { get; private set; } = default!;
    public ProjectType ProjectType { get; private set; }
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = default!;
    public string ModeOfProcurement { get; private set; } = default!;
    public bool PreProcurementConference { get; private set; }
    public string ProcurementStart { get; private set; } = default!;
    public string ProcurementEnd { get; private set; } = default!;
    public string ExpectedDelivery { get; private set; } = default!;
    public string SourceOfFunds { get; private set; } = default!;
    public decimal EstimatedBudget { get; private set; }
    public string? SupportingDocuments { get; private set; }
    public string? Remarks { get; private set; }
    public DateTimeOffset ConsolidatedAt { get; private set; }

    private AppLineItem() { }

    internal static AppLineItem FromPpmpItem(
        Guid appId,
        int itemNo,
        Ppmp ppmp,
        PpmpItem source,
        DateTimeOffset consolidatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            SourcePpmpId = ppmp.Id,
            SourcePpmpItemId = source.Id,
            SourcePpmpNumber = ppmp.PpmpNumber,
            OfficeCode = ppmp.OfficeCode,
            EndUserUnit = ppmp.EndUserUnit,
            ItemNo = itemNo,
            GeneralDescription = source.GeneralDescription,
            ProjectType = source.ProjectType,
            Quantity = source.Quantity,
            Unit = source.Unit,
            ModeOfProcurement = source.ModeOfProcurement,
            PreProcurementConference = source.PreProcurementConference,
            ProcurementStart = source.ProcurementStart,
            ProcurementEnd = source.ProcurementEnd,
            ExpectedDelivery = source.ExpectedDelivery,
            SourceOfFunds = source.SourceOfFunds,
            EstimatedBudget = source.EstimatedBudget,
            SupportingDocuments = source.SupportingDocuments,
            Remarks = source.Remarks,
            ConsolidatedAt = consolidatedAt
        };

    internal static AppLineItem Clone(Guid newAppId, int itemNo, AppLineItem source) =>
        new()
        {
            Id = Guid.NewGuid(),
            AppId = newAppId,
            SourcePpmpId = source.SourcePpmpId,
            SourcePpmpItemId = source.SourcePpmpItemId,
            SourcePpmpNumber = source.SourcePpmpNumber,
            OfficeCode = source.OfficeCode,
            EndUserUnit = source.EndUserUnit,
            ItemNo = itemNo,
            GeneralDescription = source.GeneralDescription,
            ProjectType = source.ProjectType,
            Quantity = source.Quantity,
            Unit = source.Unit,
            ModeOfProcurement = source.ModeOfProcurement,
            PreProcurementConference = source.PreProcurementConference,
            ProcurementStart = source.ProcurementStart,
            ProcurementEnd = source.ProcurementEnd,
            ExpectedDelivery = source.ExpectedDelivery,
            SourceOfFunds = source.SourceOfFunds,
            EstimatedBudget = source.EstimatedBudget,
            SupportingDocuments = source.SupportingDocuments,
            Remarks = source.Remarks,
            ConsolidatedAt = source.ConsolidatedAt
        };
}
