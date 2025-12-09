namespace dto;

using System.ComponentModel;

public class Message : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _text = "";
    private bool _isDeleted;

    public int Id { get; set; }
    public string Sender { get; set; } = "";
    public DateTime SentTime { get; set; } = DateTime.Now;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
            }
        }
    }
    public bool IsMine { get; set; }
    public bool ShowEdited => IsEdited && !IsDeleted;
    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            if (_isDeleted != value)
            {
                _isDeleted = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDeleted)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEdited)));
            }
        }
    }
    public string DisplayText => IsDeleted ? "Message deleted" : Text;
    private bool _isEdited;
    public bool IsEdited
    {
        get => _isEdited;
        set
        {
            if (_isEdited != value)
            {
                _isEdited = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEdited)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEdited)));
            }
        }
    }
    public bool IsGroup { get; set; }
    private bool _showAvatar;

    public bool ShowAvatar
    {
        get => _showAvatar;
        set
        {
            if (_showAvatar != value)
            {
                _showAvatar = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowAvatar)));
            }
        }
    }

    private bool _showTail;

    public bool ShowTail
    {
        get => _showTail;
        set
        {
            if (_showTail != value)
            {
                _showTail = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowTail)));
            }
        }
    }

    private bool _isFirstInGroup;

    public bool IsFirstInGroup
    {
        get => _isFirstInGroup;
        set
        {
            if (_isFirstInGroup != value)
            {
                _isFirstInGroup = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFirstInGroup)));
            }
        }
    }
    
    private bool _showSenderName;
    public bool ShowSenderName
    {
        get => _showSenderName;
        set
        {
            if (_showSenderName != value)
            {
                _showSenderName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSenderName)));
            }
        }
    }
    
    private double _avatarPlaceholderWidth = 0;
    public double AvatarPlaceholderWidth
    {
        get => _avatarPlaceholderWidth;
        set
        {
            if (_avatarPlaceholderWidth != value)
            {
                _avatarPlaceholderWidth = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvatarPlaceholderWidth)));
            }
        }
    }
    
    private double _messageMarginLeft = 0;
    public double MessageMarginLeft
    {
        get => _messageMarginLeft;
        set
        {
            if (_messageMarginLeft != value)
            {
                _messageMarginLeft = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageMarginLeft)));
            }
        }
    }

}