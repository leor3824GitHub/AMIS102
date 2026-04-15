namespace FSH.Modules.AssetManagement.Domain;

/// <summary>
/// Nature of a PPE Issuance Report (PPEIR) — the reason for the inter-office transfer.
/// </summary>
public enum PPEIssuanceType
{
    TransferCO  = 0,
    TransferRO  = 1,
    TransferPO  = 2,
    Donation    = 3,
    Dumping     = 4,
    Destruction = 5,
    Sale        = 6,
    Others      = 7,
}
