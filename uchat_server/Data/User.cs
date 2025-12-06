namespace uchat_server.Data;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Nickname { get; set; } = null!;
    public string HashPassword { get; set; } = null!;
    public bool IsOnline { get; set; } = false;
    public byte[]? Avatar { get; set; }

    public ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}