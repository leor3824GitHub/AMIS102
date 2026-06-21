using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Departments.DeleteDepartment;

public sealed class DeleteDepartmentCommandHandler : ICommandHandler<DeleteDepartmentCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteDepartmentCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Department {command.Id} not found.");

        department.SoftDelete(_currentUser.GetUserId().ToString());

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
