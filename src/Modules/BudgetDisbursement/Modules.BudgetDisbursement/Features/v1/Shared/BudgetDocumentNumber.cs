namespace AMIS.Modules.BudgetDisbursement.Features.v1.Shared;

/// <summary>
/// Parsing helpers for Budget Disbursement document numbers (DV / BUR). Used to seed a missing per-year
/// counter row from the numbers already issued, so allocation never re-collides with an existing number.
/// </summary>
internal static class BudgetDocumentNumber
{
    /// <summary>
    /// Extracts the trailing serial from a document number like <c>DV-2026-00042</c> → <c>42</c>.
    /// Returns 0 when the trailing segment isn't a number.
    /// </summary>
    internal static int ParseSerial(string documentNumber)
    {
        ArgumentNullException.ThrowIfNull(documentNumber);
        var lastDash = documentNumber.LastIndexOf('-');
        return lastDash >= 0 && int.TryParse(documentNumber.AsSpan(lastDash + 1), out var serial)
            ? serial
            : 0;
    }
}
