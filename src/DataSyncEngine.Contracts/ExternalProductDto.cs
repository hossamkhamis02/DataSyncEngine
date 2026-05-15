using System.Text.Json.Serialization;

namespace DataSyncEngine.Contracts;

public class ExternalProductDto
{
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("categoryCode")]
    public string CategoryCode { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stockQuantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastModifiedUtc")]
    public DateTime LastModifiedUtc { get; set; }
}
