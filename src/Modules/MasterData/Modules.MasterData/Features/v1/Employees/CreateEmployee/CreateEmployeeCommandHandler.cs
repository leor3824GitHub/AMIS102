using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Employees.CreateEmployee;

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
            command.IsActive);

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
