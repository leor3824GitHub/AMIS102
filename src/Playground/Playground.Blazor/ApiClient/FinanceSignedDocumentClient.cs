using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;

namespace AMIS.Playground.Blazor.ApiClient;

// Mirrors the Asset Register signed-document client, but targets the Finance module's
// /signed-documents endpoints and its own FinanceDocumentType enum (DV wet-signed copies).
internal interface IFinanceSignedDocumentClient
{
    Task<SignedDocumentDto?> GetAsync(FinanceDocumentType type, Guid documentId, CancellationToken ct = default);
    Task<SignedDocumentDto> UploadAsync(FinanceDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(FinanceDocumentType type, Guid documentId, CancellationToken ct = default);
}

internal sealed class FinanceSignedDocumentClient(HttpClient http) : IFinanceSignedDocumentClient
{
    private const string Base = "api/v1/finance/signed-documents";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SignedDocumentDto?> GetAsync(FinanceDocumentType type, Guid documentId, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync($"{Base}/{type}/{documentId}", ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<SignedDocumentDto> UploadAsync(
        FinanceDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(type.ToString()), "documentType");
        form.Add(new StringContent(documentId.ToString()), "documentId");

        using var resp = await http.PostAsync(Base, form, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(JsonOptions, ct).ConfigureAwait(false))!;
    }

    public Task<byte[]> DownloadAsync(FinanceDocumentType type, Guid documentId, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{type}/{documentId}/download", ct);
}
