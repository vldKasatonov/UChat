namespace uchat_server.Data;

public class Message
{
    public int Id { get; set; }

    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public bool IsText { get; set; } = true;

    public DateTime SentAt { get; set; }

    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public TextMessage? TextMessage { get; set; }
}