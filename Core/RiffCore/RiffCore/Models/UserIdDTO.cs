using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class UserIdDTO
{
    [JsonPropertyName("id")]
    public string Id { get; set; } 
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
}