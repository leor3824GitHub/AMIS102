using SQLite;

namespace Playground.Maui.Data.Models;

[Table("CachedEmployeeProfile")]
public sealed class CachedEmployeeProfile
{
    [PrimaryKey] public string UserId { get; set; } = "";
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = "";
    public string? Department { get; set; }
    public string? Position { get; set; }
    public DateTimeOffset CachedAt { get; set; }
}

[Table("CachedICS")]
public sealed class CachedICS
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string ICSNo { get; set; } = "";
    public string Date { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ExpiresOn { get; set; }
    public int ItemCount { get; set; }
    public string EmployeeId { get; set; } = "";
    public DateTimeOffset CachedAt { get; set; }
}

[Table("CachedPAR")]
public sealed class CachedPAR
{
    [PrimaryKey] public string Id { get; set; } = "";
    public string PARNo { get; set; } = "";
    public string Date { get; set; } = "";
    public string PARType { get; set; } = "";
    public int ItemCount { get; set; }
    public string EmployeeId { get; set; } = "";
    public DateTimeOffset CachedAt { get; set; }
}
