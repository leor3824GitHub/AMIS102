namespace FSH.Playground.Blazor.Components.Pages.ProcurementPlanning;

public sealed class SelectablePpmp
{
    public Guid Id { get; init; }
    public string PpmpNumber { get; init; } = string.Empty;
    public string OfficeCode { get; init; } = string.Empty;
    public string EndUserUnit { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal TotalEstimatedBudget { get; init; }
    public bool Selected { get; set; }
}
