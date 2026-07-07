using System.Security.Claims;

namespace AMIS.Blazor.Services.Downloads;

internal static class DownloadEndpoints
{
    /// <summary>
    /// Serves PDF bytes stashed by <see cref="IPdfDownloadService"/>. The browser fetches this over a
    /// normal (cookie-authenticated) HTTP request, so nothing large travels over the SignalR circuit.
    /// </summary>
    public static void MapPdfDownloadEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/download/{token}", (string token, HttpContext ctx, IPdfDownloadCache cache) =>
        {
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var entry = cache.Take(token, userId);
            if (entry is null)
            {
                return Results.NotFound();
            }

            // No download filename → Content-Disposition inline, so the browser's PDF viewer opens it in
            // the new tab (matches the previous data-URL behavior).
            return Results.File(entry.Content, entry.ContentType);
        })
        .RequireAuthorization();
    }
}
