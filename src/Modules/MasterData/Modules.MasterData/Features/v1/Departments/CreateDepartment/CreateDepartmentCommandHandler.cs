using FSH.Framework.Core.Context;
using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Data;
using FSH.Modules.MasterData.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Departments.CreateDepartment;

public sealed class CreateDepartmentCommandHandler : ICommandHandler<CreateDepartmentCommand, DepartmentReferenceDto>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateDepartmentCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<DepartmentReferenceDto> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var codeInUse = await _dbContext.Departments
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Code == command.Code, cancellationToken)
            .ConfigureAwait(false);

        if (codeInUse)
        {
            throw new FluentValidation.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(command.Code), "A department with this code already exists.")
            ]);
        }

        var department = Department.Create(command.Code, command.Name, command.Description, command.FundCluster, command.OfficeCode);
        department.CreatedBy = _currentUser.GetUserId().ToString();

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DepartmentReferenceDto(department.Id, department.Code, department.Name, department.Description, department.IsActive, department.OfficeCode);
    }
}