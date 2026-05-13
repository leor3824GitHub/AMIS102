using Mediator;

namespace AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;

public sealed record ReportSignatoryDto(
    Guid Id,
    string ReportType,
    int SortOrder,
    string Label,
    string Name,
    string Title,
    bool IsActive);

public sealed record GetReportSignatoriesQuery(string ReportType) : IQuery<List<ReportSignatoryDto>>;

public sealed record CreateReportSignatoryCommand(
    string ReportType,
    int SortOrder,
    string Label,
    string Name,
    string Title,
    bool IsActive = true) : ICommand<ReportSignatoryDto>;

public sealed record UpdateReportSignatoryCommand(
    Guid Id,
    string ReportType,
    int SortOrder,
    string Label,
    string Name,
    string Title,
    bool IsActive) : ICommand<ReportSignatoryDto>;

public sealed record DeleteReportSignatoryCommand(Guid Id) : ICommand<Unit>;

