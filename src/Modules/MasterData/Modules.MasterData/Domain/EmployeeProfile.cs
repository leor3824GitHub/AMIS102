using FSH.Framework.Core.Domain;

namespace FSH.Modules.MasterData.Domain;

public sealed class EmployeeProfile : AggregateRoot<Guid>, IAuditableEntity
{
    public string EmployeeNumber { get; private set; } = default!;
    public string? IdentityUserId { get; private set; }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? WorkEmail { get; private set; }

    public Guid OfficeId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid? DefaultUnitOfMeasureId { get; private set; }

    public string? OfficeCode { get; private set; }

    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; set; } = [];

    public Office Office { get; private set; } = default!;
    public Department Department { get; private set; } = default!;
    public Position Position { get; private set; } = default!;
    public UnitOfMeasure? DefaultUnitOfMeasure { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? DeletedOnUtc { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    public static EmployeeProfile Create(
        string employeeNumber,
        string firstName,
        string lastName,
        Guid officeId,
        Guid departmentId,
        Guid positionId,
        string? identityUserId = null,
        string? workEmail = null,
        Guid? defaultUnitOfMeasureId = null,
        bool isActive = true,
        string? officeCode = null)
    {
        return new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            LastName = lastName,
            OfficeId = officeId,
            DepartmentId = departmentId,
            PositionId = positionId,
            IdentityUserId = identityUserId,
            WorkEmail = workEmail,
            DefaultUnitOfMeasureId = defaultUnitOfMeasureId,
            OfficeCode = officeCode,
            IsActive = isActive,
            CreatedOnUtc = DateTimeOffset.UtcNow
        };
    }

    public void LinkIdentity(string identityUserId)
    {
        IdentityUserId = identityUserId;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void UnlinkIdentity()
    {
        IdentityUserId = null;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetOwnerOfficeCode(string? officeCode)
    {
        OfficeCode = officeCode;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Update(
        string employeeNumber,
        string firstName,
        string lastName,
        Guid officeId,
        Guid departmentId,
        Guid positionId,
        string? identityUserId,
        string? workEmail,
        Guid? defaultUnitOfMeasureId,
        bool isActive)
    {
        EmployeeNumber = employeeNumber;
        FirstName = firstName;
        LastName = lastName;
        OfficeId = officeId;
        DepartmentId = departmentId;
        PositionId = positionId;
        IdentityUserId = identityUserId;
        WorkEmail = workEmail;
        DefaultUnitOfMeasureId = defaultUnitOfMeasureId;
        IsActive = isActive;
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
