namespace uchat_server.Data;

public class Chat
{
    public int Id { get; set; }
    public bool IsGroup { get; set; } = false;
    public string? Name { get; set; }
    public byte[]? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}