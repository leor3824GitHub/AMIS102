namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Basis of receipt for a Supplies and Materials Receiving Report (SMRR).
/// Mirrors the checkboxes on NFA SOP GS-PD16 Exhibit 4.
/// </summary>
public enum ReceiptType
{
    Purchase = 0,
    Transfer = 1,
    Donation = 2,
    Others   = 3,
}

