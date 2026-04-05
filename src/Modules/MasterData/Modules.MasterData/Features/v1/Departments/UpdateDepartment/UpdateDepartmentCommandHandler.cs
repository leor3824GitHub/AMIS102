using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Departments.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler : ICommandHandler<UpdateDepartmentCommand, DepartmentReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateDepartmentCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<DepartmentReferenceDto> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Department {command.Id} not found.");

        var codeInUse = await _dbContext.Departments
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != command.Id && x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A department with this code already exists.")
            ]);
        }

        department.Update(command.Code, command.Name, command.Description, command.FundCluster, command.IsActive);
        department.LastModifiedBy = _currentUser.GetUserId().ToString();

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DepartmentReferenceDto(department.Id, department.Code, department.Name, department.Description, department.IsActive);
    }
}