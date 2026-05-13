namespace AMIS.Modules.AssetRegister.Contracts.v1.ValueObjects;

/// <summary>
/// Captures signatory identity + printed name + designation at the moment of
/// signing, so the audit trail survives employee renames or role changes.
/// </summary>
public sealed class EmployeeRef
{
    public Guid EmployeeId { get; private set; }
    public string PrintedName { get; private set; } = default!;
    public string? Designation { get; private set; }

    private EmployeeRef() { }

    public static EmployeeRef Create(Guid employeeId, string printedName, string? designation) =>
        new()
        {
            EmployeeId = employeeId,
            PrintedName = printedName,
            Designation = designation
        };
}

