// Extends NSwag-generated partial classes with fields added after the last client generation.
// Remove this file when the client is next regenerated from the OpenAPI spec.

namespace FSH.Playground.Blazor.ApiClient
{
    public partial class SupplyRequestDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationId")]
        public System.Guid? WarehouseLocationId { get; set; }
    }

    public partial class ApproveSupplyRequestCommand
    {
        [System.Text.Json.Serialization.JsonPropertyName("warehouseLocationId")]
        public System.Guid WarehouseLocationId { get; set; }
    }
}
