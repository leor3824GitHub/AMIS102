using AMIS.Modules.AssetManagement.Domain;
using Mediator;

namespace AMIS.Modules.AssetManagement.Features.v1.Reports.GenerateRSPIPdf;

public sealed record GenerateRSPIPdfCommand(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    AssetType? AssetType,
    bool ActiveOnly = true,
    int PageNumber = 1,
    int PageSize = 1000) : ICommand<byte[]>;

