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

public class ReconnectRequestPayload
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class CreateChatRequestPayload
{
    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("members")]
    public List<ChatMemberRequest> Members { get; set; } = new();
}

public class ChatMemberRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("has_privileges")]
    public bool HasPrivileges { get; set; }
}

public class SendTextMessageRequestPayload
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }
    
    [JsonPropertyName("sender_id")]
    public int SenderId { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class SearchUserRequestPayload
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}