using AMIS.Framework.Core.Context;
using AMIS.Framework.Core.Exceptions;
using AMIS.Modules.MasterData.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace AMIS.Modules.MasterData.Features.v1.Employees.GetMyEmployee;

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

