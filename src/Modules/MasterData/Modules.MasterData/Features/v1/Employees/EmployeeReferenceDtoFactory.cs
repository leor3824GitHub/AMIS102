using FSH.Modules.MasterData.Contracts.v1.References;
using FSH.Modules.MasterData.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MasterData.Features.v1.Employees;

internal static class EmployeeReferenceDtoFactory
{
    public static IQueryable<EmployeeProfile> IncludeReferenceData(this IQueryable<EmployeeProfile> query) =>
        query
            .Include(x => x.Office)
            .Include(x => x.Department)
            .Include(x => x.Position)
            .Include(x => x.DefaultUnitOfMeasure);

    public static EmployeeReferenceDto ToReferenceDto(this EmployeeProfile employee) =>
        new(
            employee.Id,
            employee.EmployeeNumber,
            employee.IdentityUserId,
            employee.FirstName,
            employee.LastName,
            employee.WorkEmail,
            employee.OfficeId,
            employee.Office.Code,
            employee.Office.Name,
            employee.DepartmentId,
            employee.Department.Code,
            employee.Department.Name,
            employee.PositionId,
            employee.Position.Code,
            employee.Position.Name,
            employee.DefaultUnitOfMeasureId,
            employee.DefaultUnitOfMeasure?.Code,
            employee.DefaultUnitOfMeasure?.Name,
            employee.IsActive,
            employee.OfficeCode);
}