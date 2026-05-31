using AMIS.Modules.Expendable.Contracts.v1.Requests;
using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.QuestPdfReporting.Features.v1.Expendable.PrintEmployeeIssuance;
using QuestPDF.Companion;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.Expendable.PrintEmployeeIssuance;

public sealed class EmployeeIssuancePdfDocumentPreviewTests
{
    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_EmployeeIssuance_InCompanion()
    {
        var doc = new EmployeeIssuancePdfDocument(
            records:         SampleRecords(),
            org:             SampleOrg(),
            from:            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            to:              new DateTimeOffset(2026, 1, 31, 23, 59, 0, TimeSpan.Zero),
            employeeNames:   SampleEmployeeNames(),
            departmentNames: SampleDepartmentNames());

        doc.ShowInCompanion();
    }

    private static List<EmployeeIssuanceDto> SampleRecords() =>
    [
        new(RequestId: Guid.NewGuid(),
            RequestNumber: "RIS-2026-014",
            EmployeeId: "EMP-001",
            DepartmentId: "DEPT-ADMIN",
            FulfilledOnUtc: new DateTimeOffset(2026, 1, 12, 2, 30, 0, TimeSpan.Zero),
            Items:
            [
                new(Guid.NewGuid(), "Bond Paper, A4, 80gsm", "OS-001", 5,   220.50m, 1_102.50m),
                new(Guid.NewGuid(), "Ballpen, Black, 0.5mm", "OS-018", 24,  8.75m,   210m)
            ],
            TotalValue: 1_312.50m),
        new(RequestId: Guid.NewGuid(),
            RequestNumber: "RIS-2026-027",
            EmployeeId: "EMP-014",
            DepartmentId: "DEPT-FIN",
            FulfilledOnUtc: new DateTimeOffset(2026, 1, 20, 6, 0, 0, TimeSpan.Zero),
            Items:
            [
                new(Guid.NewGuid(), "Toner Cartridge, HP 12A", "OS-095", 1, 3_250m, 3_250m)
            ],
            TotalValue: 3_250m)
    ];

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");

    private static Dictionary<string, string> SampleEmployeeNames() => new()
    {
        ["EMP-001"] = "Juan dela Cruz",
        ["EMP-014"] = "Maria Santos"
    };

    private static Dictionary<string, string> SampleDepartmentNames() => new()
    {
        ["DEPT-ADMIN"] = "Administrative Division",
        ["DEPT-FIN"]   = "Finance Division"
    };
}
