using Mediator;

namespace AMIS.Modules.ProcurementPlanning.Features.v1.Ppmps.ExportPpmpXlsx;

/// <summary>
/// Exports a PPMP as an editable .xlsx in the GPPB PPMP form layout — same columns, TOTAL BUDGET and
/// Prepared by / Submitted by blocks as the printed PDF, set up to print landscape on 8.5 × 13 in (Folio).
/// </summary>
public sealed record ExportPpmpXlsxQuery(Guid PpmpId) : IQuery<byte[]>;
