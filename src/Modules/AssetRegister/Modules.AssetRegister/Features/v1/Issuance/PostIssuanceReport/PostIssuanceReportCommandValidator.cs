using FluentValidation;
using AMIS.Modules.AssetRegister.Contracts.v1.Issuance;

namespace AMIS.Modules.AssetRegister.Features.v1.Issuance.PostIssuanceReport;

public sealed class PostIssuanceReportCommandValidator : AbstractValidator<PostIssuanceReportCommand>
{
    public PostIssuanceReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.CertifiedBy).NotNull();
        RuleFor(x => x.CertifiedBy.EmployeeId).NotEmpty().When(x => x.CertifiedBy is not null);
        RuleFor(x => x.CertifiedBy.PrintedName).NotEmpty().When(x => x.CertifiedBy is not null);
        RuleFor(x => x.PostedBy).NotNull();
        RuleFor(x => x.PostedBy.EmployeeId).NotEmpty().When(x => x.PostedBy is not null);
        RuleFor(x => x.PostedBy.PrintedName).NotEmpty().When(x => x.PostedBy is not null);
        RuleFor(x => x.PostedOn).NotEqual(default(DateOnly));
    }
}

