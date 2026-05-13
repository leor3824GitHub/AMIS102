using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.RemoveIssuanceReportLine;

public sealed class RemoveIssuanceReportLineCommandValidator : AbstractValidator<RemoveIssuanceReportLineCommand>
{
    public RemoveIssuanceReportLineCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty().WithMessage("Report ID is required.");
        RuleFor(x => x.LineId).NotEmpty().WithMessage("Line ID is required.");
    }
}

