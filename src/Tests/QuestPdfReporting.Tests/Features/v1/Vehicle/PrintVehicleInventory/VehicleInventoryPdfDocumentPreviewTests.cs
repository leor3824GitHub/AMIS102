using AMIS.Modules.MasterData.Contracts.v1.OrganizationProfile;
using AMIS.Modules.MasterData.Contracts.v1.ReportSignatories;
using AMIS.Modules.QuestPdfReporting.Features.v1.Vehicle.PrintVehicleInventory;
using AMIS.Modules.Vehicle.Contracts.v1.Vehicles;
using QuestPDF.Companion;
using Xunit;

namespace QuestPdfReporting.Tests.Features.v1.Vehicle.PrintVehicleInventory;

public sealed class VehicleInventoryPdfDocumentPreviewTests
{
    [Fact(Skip = "Preview only — remove Skip and run while QuestPDF Companion is listening.")]
    public void Show_VehicleInventory_InCompanion()
    {
        var doc = new VehicleInventoryPdfDocument(
            items:       SampleItems(),
            org:         SampleOrg(),
            signatories: SampleSignatories(),
            asOfDate:    new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));

        doc.ShowInCompanion();
    }

    private static List<MotorVehicleInventoryItemDto> SampleItems() =>
    [
        new(1, "TOYOTA HILUX CONQUEST 2.4 4x2 AT",
            MotorNumber: "2GD-A123456", ChassisNumber: "MR0FR22G701234567",
            VehicleClassification: "PICK-UP VEHICLE", PlateNumber: "SAA-1234",
            VehicleUse: "GOV'T-UV", NumberOfCylinders: 4, EngineDisplacementCC: 2393,
            FuelType: "Diesel", Year: 2023, AcquisitionCost: 1_650_000m,
            RunningCondition: "Serviceable",
            AccountableOfficer: "Juan dela Cruz", AccountableOfficerTitle: "Driver II"),
        new(1, "TOYOTA HIACE COMMUTER",
            MotorNumber: "1KD-7654321", ChassisNumber: "JTFRX22P9N1234567",
            VehicleClassification: "VAN TYPE VEHICLE", PlateNumber: "SAB-5678",
            VehicleUse: "GOV'T-UV", NumberOfCylinders: 4, EngineDisplacementCC: 2982,
            FuelType: "Diesel", Year: 2022, AcquisitionCost: 1_980_000m,
            RunningCondition: "Serviceable",
            AccountableOfficer: "Maria Santos", AccountableOfficerTitle: "Driver I"),
        new(1, "MITSUBISHI MONTERO SPORT GLS",
            MotorNumber: "4N15-987654", ChassisNumber: "MMCJNKS40NH123456",
            VehicleClassification: "SUV", PlateNumber: "SAC-9012",
            VehicleUse: "GOV'T-UV", NumberOfCylinders: 4, EngineDisplacementCC: 2442,
            FuelType: "Diesel", Year: 2021, AcquisitionCost: 1_850_000m,
            RunningCondition: "Under Repair",
            AccountableOfficer: null, AccountableOfficerTitle: null)
    ];

    private static OrganizationProfileDto SampleOrg() =>
        new(Id: Guid.NewGuid(),
            Name: "Department of Sample Affairs",
            ShortName: "DSA",
            Address: "1234 Sample Street, Manila, Philippines",
            LogoUrl: null,
            AnnexECode: "DSA-NCR");

    private static List<ReportSignatoryDto> SampleSignatories() =>
    [
        new(Guid.NewGuid(), "VehicleInventory", 1, "Prepared by:", "Ana Reyes", "Property Officer", true),
        new(Guid.NewGuid(), "VehicleInventory", 2, "Certified Correct:", "Pedro Cruz", "Admin Officer V", true),
        new(Guid.NewGuid(), "VehicleInventory", 3, "Approved by:", "Jose Rizal", "Regional Director", true)
    ];
}
