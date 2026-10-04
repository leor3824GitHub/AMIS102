using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AMIS.Blazor.Services.Downloads;

/// <summary>
/// Opens a generated PDF in a new browser tab without shipping the bytes over the SignalR circuit.
/// The bytes are stashed in <see cref="IPdfDownloadCache"/> and the browser is pointed at a token URL
/// served by the Blazor host over a normal HTTP request. Replaces the old
/// <c>data:application/pdf;base64,{Convert.ToBase64String(bytes)}</c> + <c>window.open</c> pattern.
/// </summary>
internal interface IPdfDownloadService
{
    Task OpenInNewTabAsync(byte[] content, string fileName = "report.pdf", CancellationToken cancellationToken = default);

    /// <summary>Saves any generated file (e.g. .xlsx) via the same token hand-off, served as an attachment.</summary>
    Task DownloadAsync(byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

internal sealed class PdfDownloadService : IPdfDownloadService
{
    private readonly IPdfDownloadCache _cache;
    private readonly AuthenticationStateProvider _authProvider;
    private readonly IJSRuntime _js;

    public PdfDownloadService(IPdfDownloadCache cache, AuthenticationStateProvider authProvider, IJSRuntime js)
    {
        _cache = cache;
        _authProvider = authProvider;
        _js = js;
    }

    public async Task OpenInNewTabAsync(byte[] content, string fileName = "report.pdf", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var token = await StoreAsync(content, fileName, "application/pdf");

        // Only the tiny token URL crosses the circuit; the PDF itself travels over native HTTP.
        await _js.InvokeVoidAsync("open", cancellationToken, $"/bff/download/{token}", "_blank");
    }

    public async Task DownloadAsync(byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var token = await StoreAsync(content, fileName, contentType);

        // Served with Content-Disposition: attachment, so the browser saves it and the page stays put.
        await _js.InvokeVoidAsync("open", cancellationToken, $"/bff/download/{token}", "_self");
    }

    private async Task<string> StoreAsync(byte[] content, string fileName, string contentType)
    {
        var authState = await _authProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        return _cache.Store(content, fileName, contentType, userId);
    }
}
