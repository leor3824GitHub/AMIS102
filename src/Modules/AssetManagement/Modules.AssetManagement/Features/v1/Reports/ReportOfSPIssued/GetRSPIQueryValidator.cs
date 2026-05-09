using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.ReportOfSPIssued;

public sealed class GetRSPIQueryValidator : AbstractValidator<GetRSPIQuery>
{
    public GetRSPIQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThan(0);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(q => q)
            .Must(q => !q.DateFrom.HasValue || !q.DateTo.HasValue || q.DateFrom.Value <= q.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo.");
    }
}