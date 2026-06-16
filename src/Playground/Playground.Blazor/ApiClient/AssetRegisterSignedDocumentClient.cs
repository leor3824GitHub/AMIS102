using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMIS.Modules.AssetRegister.Contracts.v1.SignedDocuments;

namespace AMIS.Playground.Blazor.ApiClient;

// Mirrors the Procurement SignedDocumentClient, but targets the Asset Register module's
// /signed-documents endpoints and its own AssetRegisterDocumentType enum.
internal interface IArSignedDocumentClient
{
    Task<SignedDocumentDto?> GetAsync(AssetRegisterDocumentType type, Guid documentId, CancellationToken ct = default);
    Task<SignedDocumentDto> UploadAsync(AssetRegisterDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(AssetRegisterDocumentType type, Guid documentId, CancellationToken ct = default);
}

internal sealed class ArSignedDocumentClient(HttpClient http) : IArSignedDocumentClient
{
    private const string Base = "api/v1/asset-register/signed-documents";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<SignedDocumentDto?> GetAsync(AssetRegisterDocumentType type, Guid documentId, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync($"{Base}/{type}/{documentId}", ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SignedDocumentDto>(JsonOptions, ct).ConfigureAwait(false);
    }

    public async Task<SignedDocumentDto> UploadAsync(
        AssetRegisterDocumentType type, Guid documentId, Stream content, string fileName, string contentType, CancellationToken ct = default)
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

    public Task<byte[]> DownloadAsync(AssetRegisterDocumentType type, Guid documentId, CancellationToken ct = default) =>
        http.GetByteArrayAsync($"{Base}/{type}/{documentId}/download", ct);
}
