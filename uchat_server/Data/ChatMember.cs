namespace uchat_server.Data;

public class ChatMember
{
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool HasPrivileges { get; set; } = false;
}