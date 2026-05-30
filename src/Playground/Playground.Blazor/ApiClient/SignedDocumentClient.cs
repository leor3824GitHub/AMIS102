using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;

namespace AMIS.Playground.Blazor.ApiClient;

internal interface ISignedDocumentClient
{
    Task<SignedDocumentDto?> GetAsync(ProcurementDocumentType type, Guid documentId, CancellationToken ct = default);
    Task<SignedDocumentDto> UploadAsync(ProcurementDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(ProcurementDocumentType type, Guid documentId, CancellationToken ct = default);
}

internal sealed class SignedDocumentClient(HttpClient http) : ISignedDocumentClient
{
    private const string Base = "api/v1/procurement/signed-documents";

    public async Task<SignedDocumentDto?> GetAsync(ProcurementDocumentType type, Guid documentId, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync($"{Base}/{type}/{documentId}", ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(ProcurementJson.Options, ct).ConfigureAwait(false);
    }

    public async Task<SignedDocumentDto> UploadAsync(
        ProcurementDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/pdf" : contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(type.ToString()), "documentType");
        form.Add(new StringContent(documentId.ToString()), "documentId");

        using var resp = await http.PostAsync(Base, form, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(ProcurementJson.Options, ct).ConfigureAwait(false))!;
    }

    public Task<byte[]> DownloadAsync(ProcurementDocumentType type, Guid documentId, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{type}/{documentId}/download", ct);
}
