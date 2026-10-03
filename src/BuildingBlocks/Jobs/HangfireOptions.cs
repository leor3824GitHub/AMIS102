namespace AMIS.Framework.Jobs;

public class HangfireOptions
{
    /// <summary>
    /// The shipped default dashboard password. Exposed as a constant so the Production startup
    /// guard can reject it — it is public knowledge (it lives in this repo and in appsettings.json),
    /// and the dashboard it protects can enqueue, requeue and delete background jobs.
    /// Mirrors <c>JwtOptions.PlaceholderSigningKey</c>.
    /// </summary>
    public const string DefaultPassword = "Secure1234!Me";

    /// <summary>The shipped default dashboard user name.</summary>
    public const string DefaultUserName = "admin";

    public string UserName { get; set; } = DefaultUserName;
    public string Password { get; set; } = DefaultPassword;
    public string Route { get; set; } = "/jobs";
}
