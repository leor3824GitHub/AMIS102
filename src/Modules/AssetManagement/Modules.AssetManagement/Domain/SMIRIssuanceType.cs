namespace AMIS.Modules.AssetManagement.Domain;

/// <summary>
/// Nature of the semi-expendable property issuance, per the PPEIR/SMIR form field
/// "Transfer to C.O. / Transfer to R.O. / Transfer to P.O. / Donation / Dumping /
///  Destruction / Sale / Others."
/// </summary>
public enum SMIRIssuanceType
{
    /// <summary>Transfer to another government office or unit.</summary>
    Transfer = 0,

    /// <summary>Donated to an external recipient.</summary>
    Donation = 1,

    /// <summary>Disposed of through dumping or destruction.</summary>
    Disposal = 2,

    /// <summary>Sold through public bidding or negotiated sale.</summary>
    Sale = 3,

    /// <summary>Any other nature of issuance not listed above.</summary>
    Others = 4,
}

