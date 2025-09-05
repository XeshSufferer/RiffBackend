using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class UserLoginData
{
    [JsonPropertyName("password")]
    public string Password { get; set; }
    [JsonPropertyName("login")]
    public string Login { get; set; }
    [JsonPropertyName("correlation_id")]
    public string CorrelationId { get; set; }
}