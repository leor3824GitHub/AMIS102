using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Playground.Blazor.ApiClient;
using MudBlazor;

namespace AMIS.Playground.Blazor.Components.Pages.Procurement;

public partial class CanvassQuotationDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public Guid CanvassRequestId { get; set; }
    [Parameter, EditorRequired] public Guid PurchaseRequestId { get; set; }
    [Parameter] public QuotationForm Form { get; set; } = new();

    [Inject] private IMaster_dataClient MasterDataClient { get; set; } = default!;
    [Inject] private ICanvassRequestClient CanvassClient { get; set; } = default!;
    [Inject] private IPurchaseRequestClient PurchaseRequestClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private SupplierDto? _selectedSupplier;
    private bool _busy;
    private bool _loadingLineItems;

    private int SelectedCount => Form.LineItems.Count(li => li.IsSelected);
    private bool AllSelected => Form.LineItems.Count > 0 && Form.LineItems.All(li => li.IsSelected);

    private void OnToggleAll(bool value)
    {
        foreach (var li in Form.LineItems)
        {
            li.IsSelected = value;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadPurchaseRequestLineItemsAsync();
    }

    private async Task LoadPurchaseRequestLineItemsAsync()
    {
        _loadingLineItems = true;
        try
        {
            var pr = await PurchaseRequestClient.GetAsync(PurchaseRequestId);
            if (pr is null)
            {
                Snackbar.Add("Linked purchase request not found.", Severity.Warning);
                Form.LineItems = new List<QLineItemRow>();
                return;
            }

            Form.LineItems = pr.LineItems
                .OrderBy(x => x.ItemNo)
                .Select(x => new QLineItemRow
                {
                    Description = x.ItemDescription,
                    Unit = x.UnitOfIssue,
                    Quantity = x.Quantity,
                    UnitPrice = x.EstimatedUnitCost
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load purchase request items: {ex.Message}", Severity.Error);
            Form.LineItems = new List<QLineItemRow>();
        }
        finally
        {
            _loadingLineItems = false;
        }
    }

    private async Task<IEnumerable<SupplierDto>> SearchSuppliers(string value, CancellationToken token)
    {
        try
        {
            var response = await MasterDataClient.SuppliersListAsync(
                keyword: string.IsNullOrWhiteSpace(value) ? null : value,
                pageNumber: 1,
                pageSize: 20,
                isActive: null,
                cancellationToken: token);

            return response?.Items ?? Enumerable.Empty<SupplierDto>();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load suppliers: {ex.Message}", Severity.Warning);
            return Enumerable.Empty<SupplierDto>();
        }
    }

    private static string FormatSupplier(SupplierDto? supplier)
    {
        if (supplier is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(supplier.Code)
            ? supplier.Name
            : $"{supplier.Name} ({supplier.Code})";
    }

    private async Task OnSupplierChanged(SupplierDto? supplier)
    {
        _selectedSupplier = supplier;

        if (supplier is null)
        {
            Form.SupplierName = string.Empty;
            Form.SupplierAddress = null;
            Form.TinNumber = null;
            return;
        }

        Form.SupplierName = supplier.Name ?? string.Empty;
        Form.SupplierAddress = supplier.Address;
        Form.TinNumber = supplier.TinNo;

        try
        {
            var details = await MasterDataClient.SuppliersGetAsync(supplier.Id);
            if (details is not null)
            {
                Form.SupplierAddress = details.Address ?? supplier.Address;
                Form.TinNumber = details.TinNo ?? supplier.TinNo;
            }
        }
        catch
        {
            // Keep autocomplete payload values when details lookup fails.
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Form.SupplierName))
        {
            Snackbar.Add("Select a supplier.", Severity.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(Form.SupplierAddress))
        {
            Snackbar.Add("Supplier address is required.", Severity.Warning);
            return;
        }
        if (!Form.QuotationDateNullable.HasValue)
        {
            Snackbar.Add("Quotation date is required.", Severity.Warning);
            return;
        }
        var selected = Form.LineItems.Where(li => li.IsSelected).ToList();
        if (selected.Count == 0)
        {
            Snackbar.Add("Select at least one item to quote.", Severity.Warning);
            return;
        }
        if (selected.Any(li => li.UnitPrice <= 0))
        {
            Snackbar.Add("Enter a unit price greater than zero for each selected item.", Severity.Warning);
            return;
        }

        _busy = true;
        try
        {
            var lineItems = selected
                .Select(li => new AddQuotationLineItemRequest(li.Description, li.Unit, li.Quantity, li.UnitPrice))
                .ToList();

            await CanvassClient.AddQuotationAsync(CanvassRequestId, new AddQuotationCommand(
                CanvassRequestId,
                Guid.NewGuid(),
                Form.SupplierName,
                Form.SupplierAddress ?? string.Empty,
                Form.TinNumber,
                DateOnly.FromDateTime(Form.QuotationDateNullable!.Value),
                Form.DeliveryTerms,
                lineItems));

            Snackbar.Add("Quotation added.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
