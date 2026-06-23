using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.Canvass;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.PurchaseRequests;
using AMIS.Playground.Blazor.ApiClient;
using MudBlazor;

namespace AMIS.Playground.Blazor.Components.Pages.Procurement;

public partial class CanvassRequestFormDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject] private ICanvassRequestClient Client { get; set; } = default!;
    [Inject] private IPurchaseRequestClient PurchaseRequestClient { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private PurchaseRequestSummaryDto? _selectedPurchaseRequest;
    private PurchaseRequestDto? _selectedPurchaseRequestDetail;
    private List<CanvassablePrLineDto> _canvassableLines = [];
    private readonly HashSet<int> _selectedItemNos = [];
    private DateTime? _returnDeadlineNullable;
    private bool _busy;

    private bool _hasUncoveredLines => _canvassableLines.Any(l => !l.IsCovered);

    private async Task<IEnumerable<PurchaseRequestSummaryDto>> SearchPurchaseRequests(string value, CancellationToken token)
    {
        try
        {
            var result = await PurchaseRequestClient.SearchAsync(
                keyword: string.IsNullOrWhiteSpace(value) ? null : value,
                status: PurchaseRequestStatus.Approved,
                page: 1,
                pageSize: 20,
                excludeFullyCanvassed: true,
                ct: token);

            return result.Items ?? Enumerable.Empty<PurchaseRequestSummaryDto>();
        }
        catch
        {
            return Enumerable.Empty<PurchaseRequestSummaryDto>();
        }
    }

    private async Task OnPurchaseRequestChanged(PurchaseRequestSummaryDto? request)
    {
        _selectedPurchaseRequest = request;
        _selectedPurchaseRequestDetail = null;
        _canvassableLines = [];
        _selectedItemNos.Clear();

        if (request is not null)
        {
            try
            {
                _selectedPurchaseRequestDetail = await PurchaseRequestClient.GetAsync(request.Id);
            }
            catch
            {
                _selectedPurchaseRequestDetail = null;
            }

            try
            {
                _canvassableLines = (await Client.GetCanvassablePrLinesAsync(request.Id)).ToList();
                // Pre-select all lines that are still available for canvassing.
                foreach (var line in _canvassableLines.Where(l => !l.IsCovered))
                {
                    _selectedItemNos.Add(line.ItemNo);
                }
            }
            catch
            {
                _canvassableLines = [];
            }
        }
    }

    private void ToggleItem(int itemNo, bool selected)
    {
        if (selected) _selectedItemNos.Add(itemNo);
        else _selectedItemNos.Remove(itemNo);
    }

    private static string FormatPurchaseRequest(PurchaseRequestSummaryDto? request)
    {
        if (request is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(request.Purpose)
            ? request.PrNumber
            : $"{request.PrNumber} - {request.Purpose}";
    }

    private async Task CreateAsync()
    {
        if (_selectedPurchaseRequest is null || !_returnDeadlineNullable.HasValue)
        {
            Snackbar.Add("Select a purchase request and return deadline.", Severity.Warning);
            return;
        }
        if (_selectedItemNos.Count == 0)
        {
            Snackbar.Add("Select at least one line item to canvass.", Severity.Warning);
            return;
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var selectedDeadline = DateOnly.FromDateTime(_returnDeadlineNullable.Value);
        if (selectedDeadline <= today)
        {
            Snackbar.Add("Return deadline must be a future date.", Severity.Warning);
            return;
        }
        _busy = true;
        try
        {
            await Client.CreateAsync(new CreateCanvassRequestCommand(
                _selectedPurchaseRequest.Id, selectedDeadline, _selectedItemNos.OrderBy(n => n).ToList()));
            Snackbar.Add("Canvass request created.", Severity.Success);
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
