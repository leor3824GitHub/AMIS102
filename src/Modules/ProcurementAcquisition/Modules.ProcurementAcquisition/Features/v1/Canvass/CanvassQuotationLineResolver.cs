using AMIS.Modules.ProcurementAcquisition.Domain.Canvass;

namespace AMIS.Modules.ProcurementAcquisition.Features.v1.Canvass;

/// <summary>
/// Shared logic for stamping a quotation's lines with the PR line they answer. Quotations are entered by
/// free-text description; the only join back to the PR line is that description. This builds the
/// description → <c>PrItemNo</c> lookup from the canvass's covered lines so AddQuotation/UpdateQuotation can
/// record the partition key on each quote line — making award, the Abstract of Canvass, and PO generation
/// robust against duplicate descriptions instead of re-matching by text at every stage.
/// </summary>
internal static class CanvassQuotationLineResolver
{
    /// <summary>
    /// Maps each covered line's normalized description to its <c>PrItemNo</c>. Throws when the canvass covers
    /// two lines with the same normalized description — that case is ambiguous and a quote line could not be
    /// attributed to a single PR line.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> BuildPrItemNoLookup(CanvassRequest canvass)
    {
        var lookup = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var li in canvass.LineItems)
        {
            var key = (li.Description ?? string.Empty).Trim().ToLowerInvariant();
            if (key.Length == 0)
                continue;

            if (!lookup.TryAdd(key, li.PrItemNo))
                duplicates.Add(li.Description ?? string.Empty);
        }

        if (duplicates.Count > 0)
            throw new InvalidOperationException(
                "This canvass covers more than one purchase request line with the same description "
                + $"({string.Join(", ", duplicates.Distinct())}), so quoted prices cannot be attributed to a "
                + "specific line. Make the line descriptions distinct before entering quotations.");

        return lookup;
    }
}
