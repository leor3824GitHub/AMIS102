using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.AnnualProcurementPlans.ExportAppXlsx;

/// <summary>
/// Exports an APP as an editable .xlsx in the GPPB APP form layout (RA 12009) — same sections, columns,
/// totals and signatories as the printed PDF, set up to print landscape on 8.5 × 13 in (Folio / Long Bond).
/// </summary>
public sealed record ExportAppXlsxQuery(Guid AppId) : IQuery<byte[]>;
