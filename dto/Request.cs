using System.Text.Json.Nodes;

namespace dto;

public class Request
{
    public CommandType Type { get; set; }
    public JsonObject? Payload { get; set; } = new JsonObject();
}

public class LoginRequestPayload
{
    public string Username { get; set; } = string.Empty;

    //TODO: hash password
    public string Password { get; set; } = string.Empty; 
}

public class RegisterRequestPayload
{
    public string Nickname { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    //TODO: hash password
    public string Password { get; set; } = string.Empty; 
}