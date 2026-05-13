using FSH.Modules.AssetManagement.Domain;
using Mediator;

namespace FSH.Modules.AssetManagement.Features.v1.Reports.GenerateRegSPIPdf;

public sealed record GenerateRegSPIPdfCommand(
    Guid EmployeeId,
    AssetType? AssetType,
    ICSStatus? Status,
    int PageNumber = 1,
    int PageSize = 1000) : ICommand<byte[]>;
