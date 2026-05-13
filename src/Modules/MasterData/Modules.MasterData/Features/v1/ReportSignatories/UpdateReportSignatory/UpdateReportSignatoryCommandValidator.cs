using FluentValidation;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;

namespace AMIS.Modules.MasterData.Features.v1.ReportSignatories.UpdateReportSignatory;

public sealed class UpdateReportSignatoryCommandValidator : AbstractValidator<UpdateReportSignatoryCommand>
{
    private static readonly string[] AllowedReportTypes = ["VehicleInventory", "PhysicalCount", "DepartmentIssuance", "StockCard", "EmployeeIssuance"];

    public UpdateReportSignatoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.ReportType)
            .NotEmpty().WithMessage("Report type is required.")
            .Must(t => AllowedReportTypes.Contains(t)).WithMessage($"Report type must be one of: {string.Join(", ", AllowedReportTypes)}.");

        RuleFor(x => x.SortOrder)
            .GreaterThan(0).WithMessage("Sort order must be greater than 0.");

        RuleFor(x => x.Label)
            .NotEmpty().WithMessage("Label is required.")
            .MaximumLength(80).WithMessage("Label must not exceed 80 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(160).WithMessage("Name must not exceed 160 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}

