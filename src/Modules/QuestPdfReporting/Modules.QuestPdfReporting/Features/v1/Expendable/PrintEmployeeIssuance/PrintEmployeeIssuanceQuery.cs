using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;

public sealed record PrintEmployeeIssuanceQuery(
    string?         EmployeeId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string          PaperSize   = "a4",
    string          Orientation = "landscape") : IQuery<byte[]>;
