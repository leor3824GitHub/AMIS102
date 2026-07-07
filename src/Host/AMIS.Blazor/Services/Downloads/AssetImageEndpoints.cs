namespace AMIS.Blazor.Services.Downloads;

internal static class AssetImageEndpoints
{
    /// <summary>
    /// Same-origin, cookie-authenticated proxy for an asset's photo. The browser points an
    /// <c>&lt;img src&gt;</c> here (so it caches by URL and only fetches on-screen rows); the Blazor host
    /// then calls the permission-gated API endpoint using the circuit's bearer token and streams the
    /// bytes back. Keeps multi-MB base64 out of both the list JSON and the SignalR circuit.
    /// </summary>
    public static void MapAssetImageEndpoints(this WebApplication app)
    {
        app.MapGet("/bff/asset-image/{id:guid}", async (
            Guid id,
            HttpClient http,
            HttpContext ctx,
            CancellationToken ct,
            string? variant = null) =>
        {
            // http is the scoped, auth-handler-wrapped client (BaseAddress = API); it injects the
            // bearer token read from the current request's auth cookie. A 403/404 from the API is
            // forwarded verbatim so the <img> simply fails to render. variant (thumb|full) selects the
            // stored size — list rows request the thumbnail, detail views the full image.
            var query = string.Equals(variant, "thumb", StringComparison.OrdinalIgnoreCase)
                ? "?variant=thumb"
                : string.Empty;
            using var resp = await http.GetAsync($"api/v1/asset-register/assets/{id}/image{query}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)resp.StatusCode);
            }

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "image/jpeg";

            // Per-asset image that rarely changes → let the browser cache it briefly. Callers append a
            // version token after edits (see AssetPhotoDialog) to bust this when the photo changes.
            ctx.Response.Headers.CacheControl = "private, max-age=300";
            return Results.File(bytes, contentType);
        })
        .RequireAuthorization();
    }
}
