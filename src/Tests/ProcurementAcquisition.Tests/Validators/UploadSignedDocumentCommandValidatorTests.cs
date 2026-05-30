using AMIS.Modules.ProcurementAcquisition.Contracts.v1.SignedDocuments;
using AMIS.Modules.ProcurementAcquisition.Features.v1.SignedDocuments.UploadSignedDocument;
using Shouldly;
using Xunit;

namespace ProcurementAcquisition.Tests.Validators;

public sealed class UploadSignedDocumentCommandValidatorTests
{
    private readonly UploadSignedDocumentCommandValidator _sut = new();

    // "%PDF-" magic header followed by arbitrary bytes.
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

    private static UploadSignedDocumentCommand Valid() => new(
        ProcurementDocumentType.PurchaseRequest, Guid.NewGuid(), "PR-2026-0001-signed.pdf", "application/pdf", PdfBytes);

    [Fact]
    public void Validate_ValidCommand_Passes() => _sut.Validate(Valid()).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_EmptyDocumentId_Fails()
    {
        var result = _sut.Validate(Valid() with { DocumentId = Guid.Empty });
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadSignedDocumentCommand.DocumentId));
    }

    [Fact]
    public void Validate_NonPdfFileName_Fails()
    {
        var result = _sut.Validate(Valid() with { FileName = "scan.jpg" });
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadSignedDocumentCommand.FileName));
    }

    [Fact]
    public void Validate_EmptyContent_Fails()
    {
        var result = _sut.Validate(Valid() with { Content = [] });
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadSignedDocumentCommand.Content));
    }

    [Fact]
    public void Validate_NonPdfContent_Fails()
    {
        // .pdf filename but the bytes are not a PDF (e.g. a renamed JPEG: 0xFF 0xD8 ...).
        var result = _sut.Validate(Valid() with { Content = [0xFF, 0xD8, 0xFF, 0xE0] });
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UploadSignedDocumentCommand.Content));
    }
}
