using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("chat_id")]
    public string ChatId { get; set; }
    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
    [JsonPropertyName("created")]
    public DateTime Created { get; set; }
    [JsonPropertyName("is_modified")]
    public bool IsModified { get; set; }
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
    
    
}