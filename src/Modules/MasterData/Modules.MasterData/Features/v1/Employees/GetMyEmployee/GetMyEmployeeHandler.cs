using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Employees.GetMyEmployee;

public sealed class GetMyEmployeeHandler(MasterDataDbContext dbContext, ICurrentUser currentUser)
    : IQueryHandler<GetMyEmployeeQuery, MyEmployeeDto>
{
    public async ValueTask<MyEmployeeDto> Handle(GetMyEmployeeQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId().ToString();

        var employee = await dbContext.Employees
            .AsNoTracking()
            .Where(x => x.IdentityUserId == userId)
            .Select(x => new MyEmployeeDto(
                x.Id,
                x.FirstName + " " + x.LastName,
                x.Department.Name,
                x.Position.Name))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return employee ?? throw new NotFoundException("No employee profile found for the current user.");
    }
}
