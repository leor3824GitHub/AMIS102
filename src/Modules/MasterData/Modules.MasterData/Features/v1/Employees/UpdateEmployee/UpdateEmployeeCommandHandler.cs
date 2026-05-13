using AMIS.Framework.Core.Context;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Employees.UpdateEmployee;

public sealed class UpdateEmployeeCommandHandler : ICommandHandler<UpdateEmployeeCommand, EmployeeReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateEmployeeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<EmployeeReferenceDto> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Employee {command.Id} not found.");

        await EnsureReferencesExist(command.OfficeId, command.DepartmentId, command.PositionId, command.DefaultUnitOfMeasureId, cancellationToken)
            .ConfigureAwait(false);

        var employeeNumberInUse = await _dbContext.Employees
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != command.Id && x.EmployeeNumber == command.EmployeeNumber, cancellationToken)
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
                .AnyAsync(x => x.Id != command.Id && x.IdentityUserId == command.IdentityUserId, cancellationToken)
                .ConfigureAwait(false);

            if (identityUserIdInUse)
            {
                throw new FluentValidation.ValidationException(
                [
                    new FluentValidation.Results.ValidationFailure(nameof(command.IdentityUserId), "This identity user is already linked to another employee.")
                ]);
            }
        }

        employee.Update(
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

        employee.LastModifiedBy = _currentUser.GetUserId().ToString();

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
                new FluentValidation.Results.ValidationFailure(nameof(UpdateEmployeeCommand.OfficeId), "Office not found.")
            ]);
        }

        if (!await _dbContext.Departments.AnyAsync(x => x.Id == departmentId, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(UpdateEmployeeCommand.DepartmentId), "Department not found.")
            ]);
        }

        if (!await _dbContext.Positions.AnyAsync(x => x.Id == positionId, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(UpdateEmployeeCommand.PositionId), "Position not found.")
            ]);
        }

        if (defaultUnitOfMeasureId.HasValue
            && !await _dbContext.UnitOfMeasures.AnyAsync(x => x.Id == defaultUnitOfMeasureId.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(UpdateEmployeeCommand.DefaultUnitOfMeasureId), "Unit of measure not found.")
            ]);
        }
    }
}

