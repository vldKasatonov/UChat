namespace dto;

using System.ComponentModel;

public class Message : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _text = "";
    private bool _isDeleted;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Sender { get; set; } = "";
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
            }
        }
    }
    public string DisplayText => IsDeleted ? "Message deleted" : Text;
    public bool IsGroup { get; set; }
}