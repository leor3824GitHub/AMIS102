using Mediator;

namespace AMIS.Modules.Expendable.Contracts.v1.Reports;

public sealed class GenerateDepartmentIssuancePdfCommand : ICommand<byte[]>
{
    public string? DepartmentId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GeneratePhysicalCountPdfCommand : ICommand<byte[]>
{
    public Guid? WarehouseLocationId { get; set; }
    public DateTime? AsOfDate { get; set; }
}

public sealed class GenerateStockCardPdfCommand : ICommand<byte[]>
{
    public Guid ProductId { get; set; }
}

public sealed class GenerateEmployeeIssuancePdfCommand : ICommand<byte[]>
{
    public string? EmployeeId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

