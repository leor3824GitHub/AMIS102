using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMIS.Modules.BudgetDisbursement.Contracts.v1.SignedDocuments;

namespace AMIS.Playground.Blazor.ApiClient;

// Mirrors the Asset Register signed-document client, but targets the Budget Disbursement module's
// /signed-documents endpoints and its own BudgetDisbursementDocumentType enum (DV wet-signed copies).
internal interface IBudgetDisbursementSignedDocumentClient
{
    Task<SignedDocumentDto?> GetAsync(BudgetDisbursementDocumentType type, Guid documentId, CancellationToken ct = default);
    Task<SignedDocumentDto> UploadAsync(BudgetDisbursementDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(BudgetDisbursementDocumentType type, Guid documentId, CancellationToken ct = default);
}

internal sealed class BudgetDisbursementSignedDocumentClient(HttpClient http) : IBudgetDisbursementSignedDocumentClient
{
    private const string Base = "api/v1/budget-disbursement/signed-documents";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SignedDocumentDto?> GetAsync(BudgetDisbursementDocumentType type, Guid documentId, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync($"{Base}/{type}/{documentId}", ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<SignedDocumentDto> UploadAsync(
        BudgetDisbursementDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default)
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

    public Task<byte[]> DownloadAsync(BudgetDisbursementDocumentType type, Guid documentId, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{type}/{documentId}/download", ct);
}