using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace dto;

public class Response
{
    public Status Status { get; set; }
    public CommandType Type { get; set; }
    public JsonObject? Payload { get; set; } = new JsonObject();
}

public class LoginResponsePayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
}

public class RegisterResponsePayload
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    
    public string Nickname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}