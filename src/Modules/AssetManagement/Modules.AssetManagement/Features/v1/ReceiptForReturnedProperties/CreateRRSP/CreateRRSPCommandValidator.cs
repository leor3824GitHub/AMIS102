using FluentValidation;

namespace FSH.Modules.AssetManagement.Features.v1.ReceiptForReturnedProperties.CreateRRSP;

public sealed class CreateRRSPCommandValidator : AbstractValidator<CreateRRSPCommand>
{
    public CreateRRSPCommandValidator()
    {
        RuleFor(x => x.RRSPNo)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^RRP-\d{4}-\d{2}-\d{4}$")
            .WithMessage("RRSPNo must follow the format RRP-YYYY-MM-NNNN (e.g. RRP-2024-01-0001).");

        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.ICSId)
            .NotEmpty();

        RuleFor(x => x.FundCluster)
            .MaximumLength(50);

        RuleFor(x => x.ReturnedByEmployeeId)
            .NotEmpty();

        RuleFor(x => x.Remarks)
            .MaximumLength(500);
    }
}
