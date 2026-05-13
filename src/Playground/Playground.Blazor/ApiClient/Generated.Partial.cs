// Extends NSwag-generated partial classes with fields added after the last client generation.
// Remove this file when the client is next regenerated from the OpenAPI spec.

namespace AMIS.Playground.Blazor.ApiClient
{
    public partial class SupplyRequestDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationId")]
        public System.Guid? WarehouseLocationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("fulfilledOnUtc")]
        public System.DateTimeOffset? FulfilledOnUtc { get; set; }
    }

    public partial class ApproveSupplyRequestCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationId")]
        public System.Guid WarehouseLocationId { get; set; }
    }

    // OfficeCode ownership fields — added after last client generation
    public partial class CategoryDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class PositionReferenceDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class CreatePositionCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class UnitOfMeasureReferenceDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class CreateUnitOfMeasureCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class SupplierDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class CreateSupplierCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class DepartmentReferenceDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class CreateDepartmentCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class OfficeReferenceDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class CreateOfficeCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }

    public partial class EmployeeReferenceDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("ownerOfficeCode")]
        public string? OwnerOfficeCode { get; set; }
    }

    public partial class CreateCategoryCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("officeCode")]
        public string? OfficeCode { get; set; }
    }
}

