using AMIS.Modules.AssetManagement.Contracts.v1.Reports;
using Mediator;

namespace AMIS.Modules.QuestPdfReporting.Features.v1.AssetManagement.PrintRSPI;

public sealed record PrintRSPIQuery(
    DateOnly?  DateFrom,
    DateOnly?  DateTo,
    AssetType? AssetType,
    bool       ActiveOnly = false,
    int        PageNumber = 1,
    int        PageSize   = 10000) : IQuery<byte[]>;
