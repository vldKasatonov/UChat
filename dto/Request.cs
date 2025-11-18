namespace dto;

using System.Text.Json.Serialization;

public class Request
{
    [JsonPropertyName("type")]
    public CommandType Type { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;
}

public class AuthRequestPayload
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    //TODO: hash password
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty; 
}