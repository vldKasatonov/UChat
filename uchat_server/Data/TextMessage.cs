namespace uchat_server.Data;

public class TextMessage
{
    public int MessageId { get; set; }
    public Message Message { get; set; } = null!;

    public string Content { get; set; } = null!;
}