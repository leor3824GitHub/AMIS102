using Mediator;

namespace FSH.Modules.MasterData.Features.v1.Suppliers.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Code,
    string Name,
    string? TinNo = null,
    string BusinessTaxType = "NON-VAT",
    string? Description = null,
    string? ContactPerson = null,
    string? Email = null,
    string? Phone = null,
    string? Address = null,
    bool IsActive = true) : ICommand;
