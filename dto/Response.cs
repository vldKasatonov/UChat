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

public class ChatErrorPayload
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty; 
}

public class LoginResponsePayload
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

public class RegisterResponsePayload
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

public class ReconnectResponsePayload
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
}

public class CreateChatResponsePayload
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }
    
    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("members")]
    public List<ChatMemberResponse> Members { get; set; } = new();
    
    // [JsonPropertyName("created_at")]
    // public DateTime CreatedAt { get; set; }
}

public class ChatMemberResponse
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("has_privileges")]
    public bool HasPrivileges { get; set; }
}

public class TextMessageResponsePayload
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }
    
    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }
    
    [JsonPropertyName("sender_id")]
    public int SenderId { get; set; }
    
    [JsonPropertyName("sender_nickname")]
    public string SenderNickname { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    [JsonPropertyName("sent_at")]
    public DateTime SentAt { get; set; }
    
    [JsonPropertyName("is_edited")]
    public bool IsEdited { get; set; }
    
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }
}

public class SearchUserResponsePayload
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;
}

public class Chats
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }
    
    [JsonPropertyName("name")]
    public string ChatName { get; set; } = string.Empty;
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }
    
    [JsonPropertyName("members")]
    public List<ChatMemberResponse> Members { get; set; } = new();
    
    [JsonPropertyName("last_message")]
    public string LastMessage { get; set; } = string.Empty;
    
    [JsonPropertyName("last_message_time")]
    public DateTime LastMessageTime { get; set; }
}

public class GetUserChatsResponsePayload
{
    [JsonPropertyName("chats")]
    public List<Chats> Chats { get; set; } = new();
}

public class ChatHistoryResponsePayload
{
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();
    
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}