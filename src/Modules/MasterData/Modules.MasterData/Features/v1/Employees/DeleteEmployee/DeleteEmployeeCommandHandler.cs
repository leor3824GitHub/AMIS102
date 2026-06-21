using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Contracts.v1.References;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Employees.DeleteEmployee;

public sealed class DeleteEmployeeCommandHandler : ICommandHandler<DeleteEmployeeCommand, Unit>
{
    private readonly MasterDataDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteEmployeeCommandHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Employee {command.Id} not found.");

        employee.SoftDelete(_currentUser.GetUserId().ToString());

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
