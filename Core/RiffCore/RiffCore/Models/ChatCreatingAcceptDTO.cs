using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class ChatCreatingAcceptDTO
{
    [JsonPropertyName("requested")]
    public User Requested { get; set; }
    [JsonPropertyName("requester")]
    public User Requester { get; set; }
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; }
}