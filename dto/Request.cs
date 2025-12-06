using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace dto;

public class Request
{
    [JsonPropertyName("type")]
    public CommandType Type { get; set; }
    
    [JsonPropertyName("payload")]
    public JsonObject? Payload { get; set; } = new JsonObject();
}

public class LoginRequestPayload
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty; 
}

public class RegisterRequestPayload
{
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}