using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintDepartmentIssuance;

public sealed record PrintDepartmentIssuanceQuery(
    string?         DepartmentId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string          PaperSize   = "a4",
    string          Orientation = "landscape",
    double          Margin      = 15d) : IQuery<byte[]>;
