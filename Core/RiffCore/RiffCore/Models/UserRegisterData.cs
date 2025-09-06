using System.Text.Json.Serialization;

namespace RiffCore.Models;

public class UserRegisterData
{
    [JsonPropertyName("correlation_id")]
    public string CorrelationID { get; set; }
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }
    
    [JsonPropertyName("login")]
    public string Login { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
}