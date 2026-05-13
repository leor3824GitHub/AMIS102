using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.RegistryOfSPIssued;

public sealed class GetRegSPIQueryValidator : AbstractValidator<GetRegSPIQuery>
{
    public GetRegSPIQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThan(0);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(q => q.EmployeeId)
            .NotEmpty();
    }
}