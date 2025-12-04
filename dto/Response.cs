using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace dto;

public class Response
{
    [JsonPropertyName("status")]
    public Status Status { get; set; }
    
    [JsonPropertyName("type")]
    public CommandType Type { get; set; }
    
    [JsonPropertyName("payload")]
    public JsonObject? Payload { get; set; } = new JsonObject();
}

public class ErrorPayload
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class LoginResponsePayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class RegisterResponsePayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}