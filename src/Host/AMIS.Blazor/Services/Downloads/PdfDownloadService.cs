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

        var authState = await _authProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var token = _cache.Store(content, fileName, "application/pdf", userId);

        // Only the tiny token URL crosses the circuit; the PDF itself travels over native HTTP.
        await _js.InvokeVoidAsync("open", cancellationToken, $"/bff/download/{token}", "_blank");
    }
}
