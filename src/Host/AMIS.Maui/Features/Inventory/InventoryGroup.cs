using AMIS.Maui.Services;

namespace AMIS.Maui.Features.Inventory;

/// <summary>
/// A titled section ("INVENTORY CUSTODIAN SLIPS" / "PROPERTY ACKNOWLEDGEMENT RECEIPTS")
/// for the grouped inventory <c>CollectionView</c>. Items are boxed because a single
/// grouped list mixes <see cref="ICSSummaryDto"/> and <see cref="PARSummaryDto"/>.
/// </summary>
public sealed class InventoryGroup(string title, IEnumerable<object> items)
    : List<object>(items)
{
    public string Title { get; } = title;
}

/// <summary>
/// Picks the ICS or PAR card template per item so both kinds can share one
/// virtualized, grouped <c>CollectionView</c>.
/// </summary>
public sealed class InventoryItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate IcsTemplate { get; set; } = null!;
    public DataTemplate ParTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        item is PARSummaryDto ? ParTemplate : IcsTemplate;
}
