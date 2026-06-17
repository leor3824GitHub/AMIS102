using FluentValidation;
using AMIS.Modules.Finance.Contracts.v1.SignedDocuments;

namespace AMIS.Modules.Finance.Features.v1.SignedDocuments.UploadSignedDocument;

public sealed class UploadSignedDocumentCommandValidator : AbstractValidator<UploadSignedDocumentCommand>
{
    public UploadSignedDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("The signed copy must be a PDF file.");
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Uploaded file is empty.")
            .Must(HasPdfHeader).WithMessage("The uploaded file is not a valid PDF.");
    }

    // PDF files begin with the magic bytes "%PDF" (0x25 0x50 0x44 0x46) — guards against a
    // mis-named non-PDF (e.g. an image renamed to .pdf) slipping into the official record.
    private static bool HasPdfHeader(byte[]? content) =>
        content is { Length: >= 4 } && content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46;
}
