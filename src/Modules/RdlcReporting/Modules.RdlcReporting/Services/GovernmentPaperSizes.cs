namespace AMIS.Modules.RdlcReporting.Services;

public static class GovernmentPaperSizes
{
    public static (string Width, string Height) A4        => ("21cm",     "29.7cm");
    public static (string Width, string Height) Legal     => ("8.5in",    "14in");
    public static (string Width, string Height) LongBond  => ("8.5in",    "13in");
    public static (string Width, string Height) HalfA4    => ("14.85cm",  "21cm");
    public static (string Width, string Height) HalfLegal => ("7in",      "8.5in");
    public static (string Width, string Height) HalfLong  => ("6.5in",    "8.5in");
}
