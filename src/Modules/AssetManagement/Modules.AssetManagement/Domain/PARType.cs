namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Indicates whether a Property Acknowledgement Receipt (PAR) is issued
/// for a newly purchased item or as a result of a transfer.
/// </summary>
public enum PARType
{
    NewPurchase = 0,
    Transfer    = 1,
}

