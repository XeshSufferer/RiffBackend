using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("chats_ids")]
    public List<string> ChatsIds { get; set; } = new List<string>();
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonIgnore]
    public string PasswordHash { get; set; }
    [JsonPropertyName("login")]
    public string Login { get; set; }
    [JsonPropertyName("created")]
    public DateTime Created {get; set;} 
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
}