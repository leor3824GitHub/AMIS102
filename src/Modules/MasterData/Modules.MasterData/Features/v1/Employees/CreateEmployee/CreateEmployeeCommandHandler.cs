using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using AMIS.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Employees.CreateEmployee;

public sealed class CreateEmployeeCommandHandler : ICommandHandler<CreateEmployeeCommand, EmployeeReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateEmployeeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<EmployeeReferenceDto> Handle(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        await EnsureReferencesExist(command.OfficeId, command.DepartmentId, command.PositionId, command.DefaultUnitOfMeasureId, cancellationToken)
            .ConfigureAwait(false);

        var employeeNumberInUse = await _dbContext.Employees
            .IgnoreQueryFilters()
            .AnyAsync(x => x.EmployeeNumber == command.EmployeeNumber, cancellationToken)
            .ConfigureAwait(false);

        if (employeeNumberInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.EmployeeNumber), "An employee with this employee number already exists.")
            ]);
        }

        if (!string.IsNullOrWhiteSpace(command.IdentityUserId))
        {
            var identityUserIdInUse = await _dbContext.Employees
                .IgnoreQueryFilters()
                .AnyAsync(x => x.IdentityUserId == command.IdentityUserId, cancellationToken)
                .ConfigureAwait(false);

            if (identityUserIdInUse)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(command.IdentityUserId), "This identity user is already linked to another employee.")
                ]);
            }
        }

        var employee = EmployeeProfile.Create(
            command.EmployeeNumber,
            command.FirstName,
            command.LastName,
            command.OfficeId,
            command.DepartmentId,
            command.PositionId,
            command.IdentityUserId,
            command.WorkEmail,
            command.DefaultUnitOfMeasureId,
            command.IsActive,
            await ResolveOwnerOfficeCodeAsync(command.OfficeCode, cancellationToken).ConfigureAwait(false));

        employee.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = await _dbContext.Employees
            .AsNoTracking()
            .IncludeReferenceData()
            .FirstAsync(x => x.Id == employee.Id, cancellationToken)
            .ConfigureAwait(false);

        return result.ToReferenceDto();
    }

    /// <summary>
    /// Resolves the owner-agency stamp for a new employee row: the caller's value when supplied,
    /// otherwise this tenant's own agency code from the Organization Profile.
    /// <para>
    /// Server-stamped rather than trusted from the client so rows are owned correctly by construction
    /// instead of depending on data entry. A null result means "shared" — under the convention shared by
    /// the other MasterData reference tables, an unstamped row is editable by any agency.
    /// </para>
    /// </summary>
    private async Task<string?> ResolveOwnerOfficeCodeAsync(string? requestedOfficeCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requestedOfficeCode))
        {
            return requestedOfficeCode.Trim();
        }

        // OrganizationProfile is .IsMultiTenant(), so this reads the current tenant's profile only.
        var annexECode = await _dbContext.OrganizationProfiles
            .AsNoTracking()
            .Select(x => x.AnnexECode)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(annexECode) ? null : annexECode.Trim();
    }

    private async Task EnsureReferencesExist(
        Guid officeId,
        Guid departmentId,
        Guid positionId,
        Guid? defaultUnitOfMeasureId,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Offices.AnyAsync(x => x.Id == officeId, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(CreateEmployeeCommand.OfficeId), "Office not found.")
            ]);
        }

        if (!await _dbContext.Departments.AnyAsync(x => x.Id == departmentId, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(CreateEmployeeCommand.DepartmentId), "Department not found.")
            ]);
        }

        if (!await _dbContext.Positions.AnyAsync(x => x.Id == positionId, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(CreateEmployeeCommand.PositionId), "Position not found.")
            ]);
        }

        if (defaultUnitOfMeasureId.HasValue
            && !await _dbContext.UnitOfMeasures.AnyAsync(x => x.Id == defaultUnitOfMeasureId.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(CreateEmployeeCommand.DefaultUnitOfMeasureId), "Unit of measure not found.")
            ]);
        }
    }
}

