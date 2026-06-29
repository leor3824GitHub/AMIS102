using AMIS.Modules.AssetRegister.Contracts.v1.Assets;

namespace AMIS.Modules.AssetRegister;

public static class AssetRegisterModuleConstants
{
    public const string ModuleName = "AssetRegister";
    public const string SchemaName = "asset_register";
    public const string MigrationsTable = "__EFMigrationsHistory_AssetRegister";

    /// <summary>COA property class code for "Motor Vehicles" (Account 10606010). Used to detect
    /// vehicle-class PPE assets eligible for fleet enrollment.</summary>
    public const string VehicleClassCode = AssetClassCodes.Vehicle;
}
