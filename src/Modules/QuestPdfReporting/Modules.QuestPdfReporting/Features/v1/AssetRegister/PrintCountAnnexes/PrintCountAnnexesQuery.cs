using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetRegister.PrintCountAnnexes;

/// <summary>
/// Renders a COA physical-count supporting annex from a session's reconciliation data:
///   - Annex B — List of PP&amp;E Found at Station (overage rows).
///   - Annex C — List of Non-Existing / Missing PP&amp;E (shortage + uncounted rows).
/// </summary>
public sealed record PrintCountAnnexesQuery(
    Guid SessionId,
    CountAnnexKind Annex,
    string PaperSize = "a4",
    string Orientation = "portrait",
    double Margin = 15d) : IQuery<byte[]>;

public enum CountAnnexKind
{
    /// <summary>Annex B — items physically present but not on the books.</summary>
    FoundAtStation = 0,
    /// <summary>Annex C — items on the books but not found (missing + uncounted).</summary>
    NonExistingMissing = 1
}
