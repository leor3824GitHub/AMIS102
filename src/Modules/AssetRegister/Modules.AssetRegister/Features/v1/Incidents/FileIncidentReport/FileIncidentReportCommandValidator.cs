using FluentValidation;
using FSH.Modules.AssetRegister.Contracts.v1.Incidents;

namespace FSH.Modules.AssetRegister.Features.v1.Incidents.FileIncidentReport;

public sealed class FileIncidentReportCommandValidator : AbstractValidator<FileIncidentReportCommand>
{
    public FileIncidentReportCommandValidator()
    {
        RuleFor(x => x.IncidentDate).NotEqual(default(DateOnly));
        RuleFor(x => x.FundCluster).NotEmpty().MaximumLength(64);
        RuleFor(x => x.DepartmentOffice).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Circumstances).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.AccountableOfficer).NotNull();
        RuleFor(x => x.AccountableOfficer.EmployeeId).NotEmpty().When(x => x.AccountableOfficer is not null);
        RuleFor(x => x.AccountableOfficer.PrintedName).NotEmpty().When(x => x.AccountableOfficer is not null);
        RuleFor(x => x.AccountableOfficerDesignation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotNull().NotEmpty();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(y => y.AssetRegistryId).NotEmpty();
        });
    }
}
