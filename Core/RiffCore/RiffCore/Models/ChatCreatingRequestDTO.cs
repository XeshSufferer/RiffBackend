using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class ChatCreatingRequestDTO
{
    [JsonPropertyName("requester_id")]
    public string RequesterId { get; set; }
    [JsonPropertyName("requested_username")]
    public string RequestedUsername { get; set; }
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
}